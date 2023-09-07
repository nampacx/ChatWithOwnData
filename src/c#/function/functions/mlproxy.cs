using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using RestSharp;

namespace Company.Function.functions
{
    public class MLProxy
    {
        private readonly MLAccess mlAccess;

        public MLProxy(MLAccess mlAccess)
        {
            this.mlAccess = mlAccess;
        }

        [FunctionName("mlproxy")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            var question = data?.question;
            var indexName = data?.index_name; 

            var body = JsonConvert.SerializeObject(new { question, chat_history = new string[0], index_name = indexName });

            var response = JsonConvert.DeserializeObject<mlResponse>( await mlAccess.GetResponseAsync(body));

            return new OkObjectResult(response);
        }
    }

    class mlResponse
    {
        public string answer { get; set; }
        public string score { get; set; }
    }
}
