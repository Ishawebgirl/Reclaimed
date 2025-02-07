using Xunit;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Reclaim.Api.Model;
using Reclaim.Api.Services;
using Microsoft.EntityFrameworkCore;
using Reclaim.Api.Dtos.AzureSearch;
using Moq;
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.AspNetCore.Http;
using System.Threading;

namespace Reclaim.Api.Tests
{
    public class ChatTest : Base
    {
        private readonly DatabaseContext _db;
        private readonly ChatService _chatService;
        private readonly SearchService _searchService;
        private readonly LogService _logService;
        private readonly Mock<OpenAIClient> _mockOpenAIClient;
        private readonly Mock<SearchClient> _mockSearchClient;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;

        public ChatTest()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _db = new DatabaseContext(options);
            _logService = new LogService(_db);

            // Set up mock HttpContextAccessor
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContext.Items["AccountID"] = 1; // Set test account ID
            httpContext.Items["Role"] = Role.Administrator; // Set test role
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Set up mock OpenAI client
            _mockOpenAIClient = new Mock<OpenAIClient>();
            SetupOpenAIMock();

            // Set up mock Search client
            _mockSearchClient = new Mock<SearchClient>();
            SetupSearchClientMock();

            // Create services with mocked dependencies
            _searchService = new SearchService(_logService, _mockOpenAIClient.Object, _mockSearchClient.Object);
            _chatService = new ChatService(_db, _mockHttpContextAccessor.Object, _logService, _searchService);
        }

        private void SetupOpenAIMock()
        {
            var response = new ChatCompletions(
                new ChatChoice[]
                {
                    BinaryData.FromString(@"{
                        ""answer"": ""The damage occurred on January 15, 2024"",
                        ""lineNumbers"": [1]
                    }"),
                    ChatRole.Assistant,
                    null,
                    1
                },
                DateTimeOffset.UtcNow,
                "gpt-4",
                null,
                null,
                null,
                null
            );

            _mockOpenAIClient
                .Setup(x => x.GetChatCompletions(
                    It.IsAny<string>(), 
                    It.IsAny<ChatCompletionsOptions>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(response, new Mock<Response>().Object));
        }

        private void SetupSearchClientMock()
        {
            var searchResults = SearchModelFactory.SearchResults(new[]
            {
                SearchModelFactory.SearchResult(new VectorDocument
                {
                    ID = Guid.NewGuid().ToString(),
                    DocumentID = 1,
                    PageNumber = 1,
                    Content = "The damage occurred on January 15, 2024",
                    BoundingBoxes = new List<string> { "10,10,100,10,100,30,10,30" }
                }, 1.0)
            }, null, null, null);

            _mockSearchClient
                .Setup(x => x.Search<VectorDocument>(
                    It.IsAny<string>(),
                    It.IsAny<SearchOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Response.FromValue(searchResults, new Mock<Response>().Object));
        }

        [Fact]
        public async Task TestBoundingBoxAccuracy()
        {
            // Arrange
            var claim = CreateTestClaim();
            var chat = await _chatService.Create(claim);

            // Create test document
            var document = new Document
            {
                ID = 1,
                ClaimID = claim.ID,
                AccountID = 1,
                Name = "Test Document",
                Path = "/test/path",
                Size = 1000,
                Description = "Test Description",
                Hash = "testhash",
                UniqueID = Guid.NewGuid(),
                Type = DocumentType.Claim
            };
            _db.Documents.Add(document);
            await _db.SaveChangesAsync();

            // Act
            var question = "When did the damage occur?";
            var result = await _searchService.Query(claim, chat, question);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("January 15, 2024", result.Answer);
            Assert.NotEmpty(result.References);
            
            var reference = result.References.First();
            Assert.Equal(1, reference.DocumentID);
            Assert.NotEmpty(reference.BoundingBoxes);
            Assert.Equal("10,10,100,10,100,30,10,30", reference.BoundingBoxes.First());
        }

        [Fact]
        public async Task TestConversationHistory()
        {
            // Arrange
            var claim = CreateTestClaim();
            var chat = await _chatService.Create(claim);

            // Create test document
            var document = new Document
            {
                ID = 1,
                ClaimID = claim.ID,
                AccountID = 1,
                Name = "Test Document",
                Path = "/test/path",
                Size = 1000,
                Description = "Test Description",
                Hash = "testhash",
                UniqueID = Guid.NewGuid(),
                Type = DocumentType.Claim
            };
            _db.Documents.Add(document);
            await _db.SaveChangesAsync();

            // Act - Ask initial question
            var initialQuestion = "What caused the damage?";
            var firstResponse = await _chatService.SubmitQuestion(chat, initialQuestion);

            // Set up mock for follow-up question
            var followUpResponse = new ChatCompletions(
                new ChatChoice[]
                {
                    BinaryData.FromString(@"{
                        ""answer"": ""The assessment was done on January 15, 2024"",
                        ""lineNumbers"": [1]
                    }"),
                    ChatRole.Assistant,
                    null,
                    1
                },
                DateTimeOffset.UtcNow,
                "gpt-4",
                null,
                null,
                null,
                null
            );

            _mockOpenAIClient
                .Setup(x => x.GetChatCompletions(
                    It.Is<string>(s => s == Setting.AzureOpenAIDeploymentName), 
                    It.Is<ChatCompletionsOptions>(o => o.Messages.Count > 2), // Verify conversation history
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(followUpResponse, new Mock<Response>().Object));

            // Ask follow-up question
            var followUpQuestion = "When was the assessment done?";
            var secondResponse = await _chatService.SubmitQuestion(chat, followUpQuestion);

            // Assert
            Assert.NotNull(firstResponse);
            Assert.NotNull(secondResponse);
            Assert.Contains("January 15, 2024", secondResponse.Text);
        }

        private Claim CreateTestClaim()
        {
            var claim = new Claim
            {
                ID = 1,
                UniqueID = Guid.NewGuid(),
                Description = "Test Claim",
                CreatedTimestamp = DateTime.UtcNow
            };

            _db.Claims.Add(claim);
            _db.SaveChanges();

            return claim;
        }
    }
}