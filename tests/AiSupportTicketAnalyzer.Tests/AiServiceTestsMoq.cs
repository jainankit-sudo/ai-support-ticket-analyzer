using System.Net;
using System.Text;
using AiSupportTicketAnalyzer.Api.AI;
using AiSupportTicketAnalyzer.Api.Data;
using AiSupportTicketAnalyzer.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;

namespace AiSupportTicketAnalyzer.Tests;

public class AiServiceTestsMoq
{
    [Fact]
    public async Task AnalyzeTicketAsync_ReturnsTicketAnalysis()
    {
        // Arrange

        var toolCallResponse = """
        {
          "choices": [
            {
              "message": {
                "role": "assistant",
                "content": null,
                "tool_calls": [
                  {
                    "id": "call_123",
                    "type": "function",
                    "function": {
                      "name": "SearchKnownIssues",
                      "arguments": "{\"searchTerm\":\"Webhook creation HTTP 500\"}"
                    }
                  }
                ]
              }
            }
          ]
        }
        """;

        var finalResponse = """
        {
          "choices": [
            {
              "message": {
                "role": "assistant",
                "content": "{\"category\":\"Integration\",\"priority\":\"High\",\"product\":\"Webhook API\",\"errorMessage\":\"HTTP 500\",\"customerImpact\":\"Unable to create webhook\",\"knownIssue\":true,\"knownIssueSummary\":\"Webhook creation can fail when the callback URL does not use HTTPS.\",\"suggestedResponse\":\"Verify that the webhook callback URL uses HTTPS and retry the request.\"}"
              }
            }
          ]
        }
        """;

        var responses = new Queue<string>();
        responses.Enqueue(toolCallResponse);
        responses.Enqueue(finalResponse);

        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var responseBody = responses.Dequeue();

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        responseBody,
                        Encoding.UTF8,
                        "application/json")
                };
            });

        var httpClient = new HttpClient(handlerMock.Object);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:BaseUrl"] = "http://localhost:11434/v1",
                ["AI:Model"] = "qwen3:8b"
            })
            .Build();

        var environment = new TestWebHostEnvironment
        {
            ContentRootPath = GetApiProjectRoot()
        };

        var repository = new KnownIssueRepository(environment);

        var aiService = new AiService(
            httpClient,
            configuration,
            repository);

        var ticket = new SupportTicket
        {
            Title = "Webhook creation failing",
            Description =
                "We are getting HTTP 500 errors when creating a webhook.",
            Product = "Webhook API"
        };

        // Act

        var result =
            await aiService.AnalyzeTicketAsync(ticket);

        // Assert

        Assert.Equal("Integration", result.Category);
        Assert.Equal("High", result.Priority);
        Assert.Equal("Webhook API", result.Product);
        Assert.True(result.KnownIssue);
        Assert.Contains("HTTPS", result.KnownIssueSummary);

        handlerMock
            .Protected()
            .Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }

    private static string GetApiProjectRoot()
    {
        var directory =
            new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            var apiProjectPath = Path.Combine(
                directory.FullName,
                "src",
                "AiSupportTicketAnalyzer.Api");

            if (Directory.Exists(apiProjectPath))
            {
                return apiProjectPath;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the AiSupportTicketAnalyzer.Api project directory.");
    }
}