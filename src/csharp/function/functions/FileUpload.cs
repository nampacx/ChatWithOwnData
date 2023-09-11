using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Company.Function
{
    /// <summary>
    /// Represents a file upload function that uploads a file to a specified directory in a cloud blob container.
    /// </summary>
    public class FileUpload
    {
        private readonly CloudBlobContainer containerClient;

        public FileUpload(CloudBlobContainer containerClient)
        {
            this.containerClient = containerClient;
        }

        /// <summary>
        /// Uploads a file to a specified directory in a cloud blob container.
        /// </summary>
        /// <param name="req">The HTTP request.</param>
        /// <param name="log">The logger.</param>
        /// <returns>An IActionResult representing the result of the upload operation.</returns>
        [FunctionName("FileUpload")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log
        )
        {
            log.LogInformation("csharp HTTP trigger function processed a request.");

            //Read form data.
            var formData = await req.ReadFormAsync();
            //Get file.
            var file = formData.Files["file"];
            formData.TryGetValue("directory", out var directory);

            var blob = containerClient.GetBlockBlobReference($"{directory}/{file.FileName}");
            await blob.UploadFromStreamAsync(file.OpenReadStream());
            return new OkObjectResult(new { blob.Uri.AbsoluteUri });
        }
    }
}
