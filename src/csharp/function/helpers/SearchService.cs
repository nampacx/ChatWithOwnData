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

/// <summary>
/// Provides methods to interact with Azure Cognitive Search service.
/// </summary>
public class SearchService
{
    private readonly string searchEndpoint;
    private readonly string searchKey;
    private readonly SearchIndexClient searchIndexClient;
    private RestClient searchRestClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchService"/> class.
    /// </summary>
    /// <param name="searchEndpoint">The endpoint of the Azure Cognitive Search service.</param>
    /// <param name="searchKey">The API key for the Azure Cognitive Search service.</param>
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

    /// <summary>
    /// Updates the search index with the given transcripts.
    /// </summary>
    /// <param name="indexName">The name of the search index.</param>
    /// <param name="log">The logger instance.</param>
    /// <param name="transcripts">The transcripts to be uploaded to the search index.</param>
    public async Task UpdateIndexAsync(string indexName, ILogger log, List<IndexObject> transcripts)
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

    /// <summary>
    /// Searches the search index with the given query vectors.
    /// </summary>
    /// <param name="queryVectors">The query vectors.</param>
    /// <param name="k">The number of search results to return.</param>
    /// <param name="indexName">The name of the search index.</param>
    /// <returns>A list of search results.</returns>
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
                    fields = $"{nameof(IndexObject.content_vector)}",
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

    /// <summary>
    /// Gets a REST request for searching the search index.
    /// </summary>
    /// <param name="indexName">The name of the search index.</param>
    /// <returns>A REST request for searching the search index.</returns>
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
