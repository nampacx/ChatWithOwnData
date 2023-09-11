using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Search.Documents.Indexes.Models;
using Newtonsoft.Json;
using RestSharp;

/// <summary>
/// Represents a builder for creating and managing Azure Search indexes.
/// </summary>
public class IndexBuilder
{
    private const string Resource = "/indexes?api-version=2023-07-01-Preview";
    private readonly string key;
    private RestClient client;

    public IndexBuilder(string endpoint, string key)
    {
        var options = new RestClientOptions(endpoint);
        client = new RestClient(options);
        this.key = key;
    }

    public static IndexDefinition BuildIndex(string name)
    {
        var transcript = new IndexDefinition()
        {
            name = name,
            similarity = new Similarity() { odatatype = "#Microsoft.Azure.Search.BM25Similarity" },
            vectorSearch = new VectorSearch()
            {
                algorithmConfigurations = new List<AlgorithmConfiguration>()
                {
                    new AlgorithmConfiguration()
                    {
                        name = "vectorconfig",
                        kind = "hnsw",
                        hnswParameters = new HnswParameters()
                        {
                            metric = "cosine",
                            m = 4,
                            efConstruction = 400,
                            efSearch = 500
                        }
                    }
                }
            },
            fields = new List<Field>()
            {
                new Field()
                {
                    name = "id",
                    type = "Edm.String",
                    key = true,
                    searchable = true,
                    retrievable = true
                },
                new Field
                {
                    name = "content_vector",
                    type = "Collection(Edm.Single)",
                    searchable = true,
                    retrievable = true,
                    vectorSearchConfiguration = "vectorconfig",
                    dimensions = 1536
                },
                new Field
                {
                    name = "origin",
                    type = "Edm.String",
                    searchable = true,
                    retrievable = true
                },
                new Field
                {
                    name = "content",
                    type = "Edm.String",
                    searchable = true,
                    retrievable = true
                }
            }
        };

        return transcript;
    }

    public async Task CreateIndexAsync(IndexDefinition index)
    {
        var str = JsonConvert.SerializeObject(index);

        var request = new RestRequest(Resource, Method.Post);
        request.AddHeader("api-key", key);
        request.AddHeader("Content-Type", "application/json");
        request.AddStringBody(str, DataFormat.Json);
        var response = await client.ExecuteAsync(request);
        response.ThrowIfError();
    }

    public async Task<bool> IndexExistsAsync(string name)
    {
        var request = new RestRequest($"/indexes/{name}?api-version=2023-07-01-Preview");
        request.AddHeader("api-key", key);
        var response = await client.ExecuteAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            return true;
        }
        else
        {
            response.ThrowIfError();
            return false;
        }
    }
}
