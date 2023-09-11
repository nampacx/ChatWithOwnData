using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Company.Function.functions
{
    /// <summary>
    /// Blob trigger function that is triggered when a new transcript is uploaded to the "transcripts" container in Azure Blob Storage.
    /// </summary>
    public class TranscriptUploadedBlobTrigger
    {
        private readonly IndexBuilder indexBuilder;
        private readonly SearchService searchService;
        private readonly LLMAccess llmAccess;

        public TranscriptUploadedBlobTrigger(
            IndexBuilder indexBuilder,
            SearchService searchService,
            LLMAccess llmAccess
        )
        {
            this.indexBuilder = indexBuilder;
            this.searchService = searchService;
            this.llmAccess = llmAccess;
        }

        /// <summary>
        /// Method that is called when a new transcript is uploaded to the "transcripts" container in Azure Blob Storage.
        /// </summary>
        /// <param name="myBlob">The uploaded transcript blob.</param>
        /// <param name="name">The name of the uploaded transcript blob.</param>
        /// <param name="log">The logger instance.</param>
        [FunctionName("TranscriptUploadedBlobTrigger")]
        public async Task Run(
            [BlobTrigger("transcripts/{name}", Connection = "AzureWebJobsStorage")] Stream myBlob,
            string name,
            ILogger log
        )
        {
            log.LogInformation(
                $"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes"
            );

            var fileContent = myBlob.StreamToString();
            var fileExtension = Path.GetExtension(name);

            var indexName = Path.GetDirectoryName(name);

            if (await indexBuilder.IndexExistsAsync(indexName))
            {
                log.LogInformation($"Index {indexName} already exists");
            }
            else
            {
                var index = IndexBuilder.BuildIndex(indexName);
                await indexBuilder.CreateIndexAsync(index);
                log.LogInformation($"Created index {indexName}");
            }

            // This line is usecase specific. It splits the file into chunks of 3 lines each.
            // If you want to use this code for your own usecase, you will have to change this line.
            var chunks = Chunker.GetChunks(fileContent, fileExtension, log);

            log.LogInformation($"Created {chunks.Count} chunks");

            List<IndexObject> transcripts = await GetIndexObjectWithVectorAsync(llmAccess, name, chunks, log);
            log.LogInformation($"Created {transcripts.Count} transcripts");
            await searchService.UpdateIndexAsync(indexName, log, transcripts);

            log.LogInformation("Trigger finished!");
        }

        /// <summary>
        /// Method that creates a list of IndexObjects with content and content vectors for each chunk of the uploaded transcript.
        /// </summary>
        /// <param name="name">The name of the uploaded transcript blob.</param>
        /// <param name="content">The content of the uploaded transcript blob.</param>
        /// <param name="log">The logger instance.</param>
        /// <returns>A list of IndexObjects with content and content vectors for each chunk of the uploaded transcript.</returns>
        private static async Task<List<IndexObject>> GetIndexObjectWithVectorAsync(
            LLMAccess llmAccess,
            string name,
            List<string> content,
            ILogger log = null
        )
        {
            var transcripts = new List<IndexObject>();
            var id = name.RemoveSpecialCharacters();
            for (var i = 0; i < content.Count; i++)
            {
                var embeddings = await llmAccess.GetEmbeddingsAsync(content[i]);

                var transcript = new IndexObject()
                {
                    content = content[i],
                    content_vector = embeddings.ToList(),
                    origin = id,
                    id = $"f-{id}-{i}"
                };

                transcripts.Add(transcript);
                log?.LogInformation($"Created transcript {transcript.id}");
            }

            return transcripts;
        }
    }
}
