
using System;
using System.Threading.Tasks;
using Azure.Search.Documents;
using Azure.AI.OpenAI;

public class SearchService {
    private readonly SearchClient _searchClient;
    private readonly OpenAIClient _openAIClient;
    
    // Fixed bounding box accuracy by converting coordinates 
    private async Task<BoundingBox> ConvertCoordinates(float[] normalized, int pageWidth, int pageHeight, int pageNumber) {
        return new BoundingBox {
            X = normalized[0] * pageWidth,
            Y = normalized[1] * pageHeight,
            Width = (normalized[2] - normalized[0]) * pageWidth,
            Height = (normalized[3] - normalized[1]) * pageHeight,
            PageNumber = pageNumber
        };
    }

    // Improved conversation history support
    private async Task<string> GetChatResponse(string query, List<ChatMessage> history) {
        history = history.TakeLast(5).ToList(); // Keep last 5 messages for context
        
        var chatCompletions = await _openAIClient.GetChatCompletionsAsync(
            deploymentOrModelName:  gpt-4,
            new ChatCompletionsOptions {
                Messages = history,
                Temperature = 0.7f,
                MaxTokens = 800
            });
        return chatCompletions.Value.Choices[0].Message.Content;
    }

    // Enhanced search with context and accurate citations
    public async Task<SearchResult> SearchWithContext(string query, List<ChatMessage> history) {
        var searchResults = await _searchClient.SearchAsync<VectorDocument>(query);
        var citations = new List<Citation>();
        
        foreach (var result in searchResults.Value.GetResults()) {
            var doc = result.Document;
            var box = await ConvertCoordinates(doc.Coordinates, doc.PageWidth, doc.PageHeight, doc.PageNumber);
            citations.Add(new Citation {
                Text = doc.Content,
                BoundingBox = box,
                PageNumber = doc.PageNumber,
                Score = result.Score ?? 0
            });
        }
        
        var chatResponse = await GetChatResponse(query, history);
        
        return new SearchResult {
            Answer = chatResponse,
            Citations = citations.OrderByDescending(c => c.Score).Take(3).ToList()
        };
    }
}
