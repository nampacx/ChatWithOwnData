using System.Threading.Tasks;
using RestSharp;

public class MLAccess
{
    private readonly string authorizationKey;
    private readonly string modelName;
    private RestClient client;

    public MLAccess(string modelName, string authorizationKey, string endpoint)
    {
        this.modelName = modelName;
        this.authorizationKey = authorizationKey;
        var options = new RestClientOptions(endpoint);
        client = new RestClient(options);
    }

    public async Task<string> GetResponseAsync(string body)
    {
        var request = GetRestRequest();
        request.AddStringBody(body, DataFormat.Json);
        RestResponse response = await client.ExecuteAsync(request);

        return response.Content;
    }

    public RestRequest GetRestRequest()
    {
        var request = new RestRequest("/score", Method.Post);
        request.AddHeader("azureml-model-deployment", modelName);
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Authorization", $"Bearer {authorizationKey}");
        return request;
    }
}
