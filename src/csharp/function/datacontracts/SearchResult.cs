// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
using System.Collections.Generic;
using Newtonsoft.Json;

public class SearchResults
{
    [JsonProperty("@odata.context")]
    public string odatacontext { get; set; }

    [JsonProperty("@search.nextPageParameters")]
    public SearchNextPageParameters searchnextPageParameters { get; set; }

    [JsonProperty("value")]
    public List<SearchResult> value { get; set; }

    [JsonProperty("@odata.nextLink")]
    public string odatanextLink { get; set; }
}

public class SearchNextPageParameters
{
    public string search { get; set; }
    public string select { get; set; }
    public int skip { get; set; }
}

public class SearchResult
{
    [JsonProperty("@search.score")]
    public float searchscore { get; set; }
    public string origin { get; set; }
    public string content { get; set; }
}
