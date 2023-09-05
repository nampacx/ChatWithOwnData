using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var fileExtension = Path.GetExtension(name);
            List<string> lines = null;
            if (fileExtension == ".txt")
            {
                lines = ParseTXT(fileContent);
                log.LogInformation($"Parsed {lines.Count} lines from TXT file");
            }
            else if (fileExtension == ".vtt")
            {
                lines = ParseVTT(fileContent);
                log.LogInformation($"Parsed {lines.Count} lines from VTT file");
            }
            else
            {
                log.LogInformation("File extension not supported");
                return;
            }

            var chunks = CreateChunks(lines);

            log.LogInformation($"Created {chunks.Count} chunks");

            List<transcript> transcripts = await GetTranscriptsWithEmbedding(name, chunks, log);
            log.LogInformation($"Created {transcripts.Count} transcripts");
            UpdateIndex(log, transcripts);

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

        private static string GetID(string name)
        {
            var pattern = "[^a-zA-Z0-9]+";
            var replacement = "";
            var output = Regex.Replace(Path.GetFileNameWithoutExtension(name), pattern, replacement);
            return output;
        }

        private static List<string> ParseTXT(string fileContent)
        {
            return fileContent.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private static List<string> ParseVTT(string fileContent)
        {
            return fileContent.Split(new[] { $"{Environment.NewLine}{Environment.NewLine}" }, StringSplitOptions.RemoveEmptyEntries).Select(f =>
            {
                var lines = f.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Where(l => !l.Contains("-->")).Select(l => l.Replace("<v ", "").Replace("</v>", "").Replace(">", ": ")).ToList();
                return string.Join(Environment.NewLine, lines);
            }).ToList();
        }

        private void UpdateIndex(ILogger log, List<transcript> transcripts)
        {
            var batch = IndexDocumentsBatch.Create(transcripts.Select(t => IndexDocumentsAction.Upload(t)).ToArray());
            try
            {
                var searchClient = searchIndexClient.GetSearchClient(indexName);
                var result = searchClient.IndexDocuments(batch);

                if (!result.Value.Results.All(r => r.Succeeded))
                {
                    log.LogError($"Failed to index some of the documents: {string.Join(", ", result.Value.Results.Where(r => !r.Succeeded).Select(r => r.Key))}");
                }
                else
                {
                    log.LogInformation($"Indexed {transcripts.Count} documents");
                }
            }
            catch (Exception e)
            {
                // Sometimes when your Search service is under load, indexing will fail for some of the documents in
                // the batch. Depending on your application, you can take compensating actions like delaying and
                // retrying. For this simple demo, we just log the failed document keys and continue.
                log.LogCritical($"{e.Message}");
            }
        }

        private async Task<List<transcript>> GetTranscriptsWithEmbedding(string name, List<string> chunks, ILogger log = null)
        {
            var transcripts = new List<transcript>();
            var id = GetID(name);
            for (var i = 0; i < chunks.Count; i++)
            {
                var opntions = new EmbeddingsOptions(chunks[i]);

                var result = await client.GetEmbeddingsAsync(EmbeddingModelName, opntions);
                var embeddings = result.Value.Data[0].Embedding;

                var transcript = new transcript()
                {
                    content = chunks[i],
                    content_vector = embeddings.ToList(),
                    speaker = chunks[i].Split(":")[0],
                    origin = id,
                    id = $"f-{id}-{i}"
                };

                transcripts.Add(transcript);
                log?.LogInformation($"Created transcript {transcript.id}");
            }

            return transcripts;
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
