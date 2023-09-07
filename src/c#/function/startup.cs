using System;
using System.Reflection;
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


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
                        var aoaiEndpoint = new Uri(_config.GetValue<string>("OPENAI_ENDPOINT"));
                        var aoaiCredentials = new AzureKeyCredential(_config.GetValue<string>("OPENAI_KEY"));
                        return new OpenAIClient(aoaiEndpoint, aoaiCredentials);
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
              var endpoint =_config.GetValue<string>("AZURE_SEARCH_ENDPOINT");
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