using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Company.Function
{
    public class Ask
    {
        private readonly LLMAccess llmAccess;
        private readonly SearchService searchService;

        public Ask(LLMAccess llmAccess, SearchService   searchService)
        {
            this.llmAccess = llmAccess;
            this.searchService = searchService;
        }

        [FunctionName("Ask")]
        public  async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject<Request>(requestBody);
            var question = data.question;
            var index_name = data.index_name; 

            var embeddings = await llmAccess.GetEmbeddingsAsync(question);

            List<SearchResult> searchResults = await searchService.SearchAsync(embeddings, 1, index_name);
            var pastMeetingsTranscripts = searchResults.Select(r => $"Meeting tile: {r.origin}{Environment.NewLine}{r.content}").ToList();
            var answer = await llmAccess.ExecutePromptAsync(pastMeetingsTranscripts, question);

            return new OkObjectResult(new {question, answer});
        }

        class Request{
            public string question { get; set; }
            public string index_name { get; set; }
        }
    }
}
