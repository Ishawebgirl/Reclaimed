
using System;
using System.Threading.Tasks;
using Azure.Search.Documents;
using Azure.AI.OpenAI;

public class SearchService {
    private readonly SearchClient _searchClient;
    private readonly OpenAIClient _openAIClient;
    
    // Improved bounding box accuracy by converting normalized coordinates to absolute
    private async Task<BoundingBox> ConvertCoordinates(float[] normalized, int pageWidth, int pageHeight) {
        return new BoundingBox {
            X = normalized[0] * pageWidth,
            Y = normalized[1] * pageHeight,
            Width = (normalized[2] - normalized[0]) * pageWidth,
            Height = (normalized[3] - normalized[1]) * pageHeight
        };
    }

    // Added conversation history support
    private async Task<string> GetChatResponse(string query, List<ChatMessage> history) {
        var chatCompletions = await _openAIClient.GetChatCompletionsAsync(
            deploymentOrModelName:  gpt-4,
            new ChatCompletionsOptions {
                Messages = history,
                Temperature = 0.7f,
                MaxTokens = 800
            });
        return chatCompletions.Value.Choices[0].Message.Content;
    }
}
