using System;
using System.Reflection;
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using RestSharp;


[assembly: FunctionsStartup(typeof(Company.Function.Startup))]

namespace Company.Function
{
    public class Startup : FunctionsStartup
    {
        private IConfigurationRoot _config;

        public override void Configure(IFunctionsHostBuilder builder)
        {

            builder.Services.AddSingleton(p =>
                    {
                        var embeddingModelName = _config.GetValue<string>("EMBEDDING_MODEL_NAME");
                        var chatModelName = _config.GetValue<string>("CHAT_MODEL_NAME");
                        var aoaiEndpoint = new Uri(_config.GetValue<string>("OPENAI_ENDPOINT"));
                        var aoaiCredentials = new AzureKeyCredential(_config.GetValue<string>("OPENAI_KEY"));
                        var openAIClient = new OpenAIClient(aoaiEndpoint, aoaiCredentials);

                        return new LLMAccess(openAIClient, embeddingModelName, chatModelName);
                    });

            builder.Services.AddSingleton(p =>
          {
              // Get the service endpoint and API key from the environment
              var endpoint = new Uri(_config.GetValue<string>("AZURE_SEARCH_ENDPOINT"));
              var key = _config.GetValue<string>("AZURE_SEARCH_KEY");

              return new SearchIndexClient(endpoint, new AzureKeyCredential(key));
          });

            builder.Services.AddSingleton(p =>
       {
           // Get the service endpoint and API key from the environment
           var storageAccountConnectionString = _config.GetValue<string>("AzureWebJobsStorage");
           var containerName = _config.GetValue<string>("TRANSCRIPT_CONTAINER");
           var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
           var blobClient = storageAccount.CreateCloudBlobClient();
           var container = blobClient.GetContainerReference(containerName);
           return container;
       });

            builder.Services.AddSingleton(p =>
            {
                return new MLAccess(_config.GetValue<string>("ML_MODEL_NAME"), _config.GetValue<string>("ML_AUTHORIZATION_KEY"), _config.GetValue<string>("ML_ENDPOINT"));
            });

            builder.Services.AddSingleton(p =>
            {
                // Get the service endpoint and API key from the environment
                var endpoint = _config.GetValue<string>("AZURE_SEARCH_ENDPOINT");
                var key = _config.GetValue<string>("AZURE_SEARCH_KEY");

                return new SearchService(endpoint, key);
            });


            builder.Services.AddSingleton(p =>
        {
            // Get the service endpoint and API key from the environment
            var endpoint = _config.GetValue<string>("AZURE_SEARCH_ENDPOINT");
            var key = _config.GetValue<string>("AZURE_SEARCH_KEY");

            return new IndexBuilder(endpoint, key);
        });
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();
            var env = context.EnvironmentName;

            // add azure configuration
            if (env.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                builder.ConfigurationBuilder.AddUserSecrets(Assembly.GetExecutingAssembly(), true, true);
            }
            _config = builder.ConfigurationBuilder.Build();
        }
    }
}