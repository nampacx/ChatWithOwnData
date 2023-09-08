using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;

public class SearchService
{
    private readonly string searchEndpoint;
    private readonly string searchKey;
    private readonly SearchIndexClient searchIndexClient;
    private RestClient searchRestClient;

    public SearchService(string searchEndpoint, string searchKey)
    {
        this.searchEndpoint = searchEndpoint;
        this.searchKey = searchKey;
        searchIndexClient = new SearchIndexClient(
            new Uri(searchEndpoint),
            new AzureKeyCredential(searchKey)
        );

        var searchClientOptions = new RestClientOptions(searchEndpoint);
        searchRestClient = new RestClient(searchClientOptions);
    }

    public async Task UpdateIndexAsync(string indexName, ILogger log, List<Transcript> transcripts)
    {
        var batch = IndexDocumentsBatch.Create(
            transcripts.Select(t => IndexDocumentsAction.Upload(t)).ToArray()
        );
        try
        {
            var searchClient = searchIndexClient.GetSearchClient(indexName);
            var result = await searchClient.IndexDocumentsAsync(batch);

            if (!result.Value.Results.All(r => r.Succeeded))
            {
                log.LogError(
                    $"Failed to index some of the documents: {string.Join(", ", result.Value.Results.Where(r => !r.Succeeded).Select(r => r.Key))}"
                );
            }
            else
            {
                log.LogInformation($"Indexed {transcripts.Count} documents");
            }
        }
        catch (Exception e)
        {
            log.LogCritical($"{e.Message}");
        }
    }

    public async Task<List<SearchResult>> SearchAsync(
        IReadOnlyList<float> queryVectors,
        int k,
        string indexName
    )
    {
        var requestBody = new
        {
            vectors = new[]
            {
                new
                {
                    value = queryVectors,
                    fields = $"{nameof(Transcript.content_vector)}",
                    k
                }
            },
            select = "content,origin"
        };

        var request = GetSearchRestRequest(indexName);
        request.AddJsonBody(requestBody);
        RestResponse response = await searchRestClient.ExecuteAsync(request);
        var searchResult = JsonConvert.DeserializeObject<SearchResults>(response.Content);

        return searchResult.value;
        ;
    }

    public RestRequest GetSearchRestRequest(string indexName)
    {
        var request = new RestRequest(
            $"/indexes/{indexName}/docs/search?api-version=2023-07-01-Preview",
            Method.Post
        );
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("api-key", $"{searchKey}");
        return request;
    }
}
