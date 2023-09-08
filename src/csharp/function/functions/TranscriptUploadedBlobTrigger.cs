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
    public class TranscriptUploadedBlobTrigger
    {
        private const int MaxChunkSize = 3;
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

        [FunctionName("TranscriptUploadedBlobTrigger")]
        public async Task Run(
            [BlobTrigger("transcripts/{name}", Connection = "AzureWebJobsStorage")] Stream myBlob,
            string name,
            ILogger log
        )
        {
            log.LogInformation(
                $"csharp Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes"
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

            List<string> lines = null;
            if (fileExtension == ".txt")
            {
                lines = fileContent.ParseTXT();
                log.LogInformation($"Parsed {lines.Count} lines from TXT file");
            }
            else if (fileExtension == ".vtt")
            {
                lines = fileContent.ParseVTT();
                log.LogInformation($"Parsed {lines.Count} lines from VTT file");
            }
            else
            {
                log.LogInformation("File extension not supported");
                return;
            }

            var chunks = CreateChunks(lines);

            log.LogInformation($"Created {chunks.Count} chunks");

            List<Transcript> transcripts = await GetTranscriptsWithEmbedding(name, chunks, log);
            log.LogInformation($"Created {transcripts.Count} transcripts");
            await searchService.UpdateIndexAsync(indexName, log, transcripts);

            log.LogInformation("Trigger finished!");
        }

        private static List<string> CreateChunks(List<string> lines)
        {
            var chunks = new List<string>();
            for (var i = 0; i < lines.Count; i++)
            {
                chunks.Add(string.Join(Environment.NewLine, lines.Skip(i).Take(MaxChunkSize)));
            }

            return chunks;
        }

        private async Task<List<Transcript>> GetTranscriptsWithEmbedding(
            string name,
            List<string> chunks,
            ILogger log = null
        )
        {
            var transcripts = new List<Transcript>();
            var id = name.RemoveSpecialCharacters();
            for (var i = 0; i < chunks.Count; i++)
            {
                var embeddings = await llmAccess.GetEmbeddingsAsync(chunks[i]);

                var transcript = new Transcript()
                {
                    content = chunks[i],
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
