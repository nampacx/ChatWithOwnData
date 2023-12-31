using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Identity.Client;

/// <summary>
/// Provides access to OpenAI's GPT-3 language model for generating responses to questions based on past meeting transcripts and chat history.
/// </summary>
public class LLMAccess
{
    // This line describes the behavior of the AI assistant. Feel free to change it to whatever you like.
     private const string SystemDescription =
         "You are an AI meeting minutes assistant that helps people find information in past meeting transcripts shared with you below. When answering questions try to mention the person who said what you are referring to as well as the meeting it was said in.";


    private readonly OpenAIClient client;
    private readonly string embeddingModelName;
    private readonly string chatModelName;

    public LLMAccess(OpenAIClient client, string embeddingModelName, string chatModelName)
    {
        this.client = client;
        this.embeddingModelName = embeddingModelName;
        this.chatModelName = chatModelName;
    }

    public async Task<IReadOnlyList<float>> GetEmbeddingsAsync(string textForEmbedding)
    {
        var options = new EmbeddingsOptions(textForEmbedding);

        var result = await client.GetEmbeddingsAsync(embeddingModelName, options);
        return result.Value.Data[0].Embedding;
    }

    public async Task<string> ExecutePromptAsync(
        List<string> pastMeetingTranscripts,
        string question,
        List<AskChatMessage> chatHistory
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine(SystemDescription);
        sb.AppendLine("Past meeting transcripts:");
        sb.AppendLine(string.Join(Environment.NewLine, pastMeetingTranscripts));

        if (chatHistory != null)
        {
            sb.AppendLine("Chat history:");
            foreach (var chatMessage in chatHistory)
            {
                sb.AppendLine($"{chatMessage.role}: {chatMessage.content}");
            }
        }

        var messages = new List<ChatMessage>()
        {
            new ChatMessage("system", sb.ToString()),
            new ChatMessage("user", question)
        };

        var options = new ChatCompletionsOptions(messages)
        {
            MaxTokens = 1816,
            Temperature = 0f,
            FrequencyPenalty = 0.0f,
            PresencePenalty = 0.0f,
        };

        var chatResponse = await client.GetChatCompletionsAsync(chatModelName, options);

        return chatResponse.Value.Choices.FirstOrDefault()?.Message.Content;
    }
}
