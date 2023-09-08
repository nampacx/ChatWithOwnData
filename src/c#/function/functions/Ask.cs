using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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

            var response = await searchService.SearchAsync(embeddings, 1, index_name);

            return new OkObjectResult(response);
        }

        class Request{
            public string question { get; set; }
            public string index_name { get; set; }
        }
    }
}
