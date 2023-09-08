using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.AI.OpenAI;

public class LLMAccess
{
    private readonly OpenAIClient client;
    private readonly string embeddingModelName;

    public LLMAccess(OpenAIClient client, string embeddingModelName)
    {
        this.client = client;
        this.embeddingModelName = embeddingModelName;
    }

    public async Task<IReadOnlyList<float>> GetEmbeddingsAsync(string textForEmbedding)
    {
        var options = new EmbeddingsOptions(textForEmbedding);

        var result = await client.GetEmbeddingsAsync(embeddingModelName, options);
        return result.Value.Data[0].Embedding;
    }
}