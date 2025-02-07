
using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Azure.AI.OpenAI;

public class ChatTests {
    [Fact]
    public async Task BoundingBoxConversion_ShouldBeAccurate() {
        // Arrange
        var searchService = new SearchService();
        var normalizedCoords = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };
        var pageWidth = 1000;
        var pageHeight = 800;

        // Act
        var result = await searchService.ConvertCoordinates(normalizedCoords, pageWidth, pageHeight);

        // Assert
        Assert.Equal(100, result.X);
        Assert.Equal(160, result.Y);
        Assert.Equal(200, result.Width);
        Assert.Equal(160, result.Height);
    }

    [Fact]
    public async Task ChatHistory_ShouldMaintainContext() {
        // Arrange
        var searchService = new SearchService();
        var history = new List<ChatMessage> {
            new ChatMessage(ChatRole.User,  What is in the claim?),
            new ChatMessage(ChatRole.Assistant, The claim shows water damage.),
            new ChatMessage(ChatRole.User, When did it occur?)
        };

        // Act
        var result = await searchService.SearchWithContext(What was damaged?, history);

        // Assert
        Assert.NotNull(result.Answer);
        Assert.NotEmpty(result.Citations);
    }
}
