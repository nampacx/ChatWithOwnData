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
    /// <summary>
    /// Represents a class that handles HTTP requests to ask a question and get an answer.
    /// </summary>
    public class Ask
    {
        private readonly LLMAccess llmAccess;
        private readonly SearchService searchService;

        /// <summary>
        /// Initializes a new instance of the <see cref="Ask"/> class.
        /// </summary>
        /// <param name="llmAccess">The LLM access.</param>
        /// <param name="searchService">The search service.</param>
        public Ask(LLMAccess llmAccess, SearchService searchService)
        {
            this.llmAccess = llmAccess;
            this.searchService = searchService;
        }

        /// <summary>
        /// Handles the HTTP request to ask a question and get an answer.
        /// </summary>
        /// <param name="req">The HTTP request.</param>
        /// <param name="log">The logger.</param>
        /// <returns>The HTTP response.</returns>
        [FunctionName("Ask")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log
        )
        {
            log.LogInformation("csharp HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject<AskRequest>(requestBody);
            var question = data.question;
            var index_name = data.index_name;
            var chat_history = data.chat_history;

            var embeddings = await llmAccess.GetEmbeddingsAsync(question);

            List<SearchResult> searchResults = await searchService.SearchAsync(
                embeddings,
                1,
                index_name
            );
            var pastMeetingsTranscripts = searchResults
                .Select(r => $"Meeting tile: {r.origin}{Environment.NewLine}{r.content}")
                .ToList();
            var answer = await llmAccess.ExecutePromptAsync(pastMeetingsTranscripts, question, chat_history);
            var response = new { question, answer };
            log.LogInformation($"Response: {JsonConvert.SerializeObject(response)}");
            return new OkObjectResult(response);
        }

    
    }
}
