using System.Collections.Generic;
using Azure.Search.Documents.Indexes;

public class Transcript
        {
            [SearchableField(IsKey = true)]
            public string id { get; set; }

            public string content { get; set; }
            public List<float> content_vector { get; set; }

            public string origin { get; set; }

        }