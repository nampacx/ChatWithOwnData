using System.Collections.Generic;
using Newtonsoft.Json;

public class AlgorithmConfiguration
{
    public string name { get; set; }
    public string kind { get; set; }
    public HnswParameters hnswParameters { get; set; }
}

public class Field
{
    public string name { get; set; }
    public string type { get; set; }
    public bool searchable { get; set; }
    public bool filterable { get; set; }
    public bool retrievable { get; set; }
    public bool sortable { get; set; }
    public bool facetable { get; set; }
    public bool key { get; set; }
    public object indexAnalyzer { get; set; }
    public object searchAnalyzer { get; set; }
    public string analyzer { get; set; }
    public object normalizer { get; set; }
    public int? dimensions { get; set; }
    public string vectorSearchConfiguration { get; set; }
    public List<object> synonymMaps { get; set; } = new List<object>();
}

public class HnswParameters
{
    public string metric { get; set; }
    public int m { get; set; }
    public int efConstruction { get; set; }
    public int efSearch { get; set; }
}

public class IndexDefinition
{
    public string name { get; set; }
    public List<Field> fields { get; set; }
    public Similarity similarity { get; set; }
    public object semantic { get; set; }
    public VectorSearch vectorSearch { get; set; }
}

public class Similarity
{
    [JsonProperty("@odata.type")]
    public string odatatype { get; set; }
    public object k1 { get; set; }
    public object b { get; set; }
}

public class VectorSearch
{
    public List<AlgorithmConfiguration> algorithmConfigurations { get; set; }
}
