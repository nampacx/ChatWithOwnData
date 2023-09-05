using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        private const string EmbeddingModelName = "embedding";
        private readonly OpenAIClient client;
        private readonly SearchIndexClient searchIndexClient;
        private readonly string indexName = "transcripts";
        public TranscriptUploadedBlobTrigger(OpenAIClient client, SearchIndexClient searchIndexClient)
        {
            this.client = client;
            this.searchIndexClient = searchIndexClient;
        }


        [FunctionName("TranscriptUploadedBlobTrigger")]
        public async Task Run([BlobTrigger("transcripts/{name}", Connection = "AzureWebJobsStorage")] Stream myBlob, string name, ILogger log)
        {



            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            var fileContent = StreamToString(myBlob);

            var lines = fileContent.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var chunks = new List<string>();
            for (var i = 0; i < lines.Count; i++)
            {
                chunks.Add(string.Join(Environment.NewLine, lines.Skip(i).Take(3)));
            }


            var transcripts = new List<transcript>();
            for (var i = 0; i < chunks.Count; i++)
            {
                var opntions = new EmbeddingsOptions(chunks[i]);

                var result = await client.GetEmbeddingsAsync(EmbeddingModelName, opntions);
                var embeddings = result.Value.Data[0].Embedding;

                var transcript = new transcript()
                {
                    content = chunks[i],
                    content_vector = embeddings.ToList(),
                    speaker = "N.N.",
                    origin = name,
                    id = $"f{i}"
                };

                transcripts.Add(transcript);
            }

            var batch = IndexDocumentsBatch.Create(transcripts.Select(t => IndexDocumentsAction.Upload(t)).ToArray());


            try
            {
                var searchClient = searchIndexClient.GetSearchClient(indexName);
                var result = searchClient.IndexDocuments(batch);
            }
            catch (Exception e)
            {
                // Sometimes when your Search service is under load, indexing will fail for some of the documents in
                // the batch. Depending on your application, you can take compensating actions like delaying and
                // retrying. For this simple demo, we just log the failed document keys and continue.
                log.LogError($"{e.Message}",e);
            }

            log.LogInformation("Trigger finished!");
        }

        public static string StreamToString(Stream stream)
        {
            stream.Position = 0;
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        class transcript
        {
            public string id { get; set; }
            public string content { get; set; }
            public List<float> content_vector { get; set; }

            public string origin { get; set; }

            public string speaker { get; set; }
        }
    }
}
