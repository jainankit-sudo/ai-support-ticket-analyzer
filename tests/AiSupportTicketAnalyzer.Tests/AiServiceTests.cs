using System.Net;
using System.Text;
using AiSupportTicketAnalyzer.Api.AI;
using AiSupportTicketAnalyzer.Api.Data;
using AiSupportTicketAnalyzer.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace AiSupportTicketAnalyzer.Tests;

public class AiServiceTests
{
    [Fact]
    public async Task AnalyzeTicketAsync_ReturnsTicketAnalysis()
    {
        // Arrange
        var responses = new Queue<string>();

        // First response: AI asks to search the known issues database.
        responses.Enqueue("""
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
        """);

        // Second response: AI uses the tool result and returns final analysis.
        responses.Enqueue("""
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
        """);

        var handler = new FakeHttpMessageHandler(responses);

        var httpClient = new HttpClient(handler);

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
            Description = "We are getting HTTP 500 errors when creating a webhook.",
            Product = "Webhook API"
        };

        // Act
        var result = await aiService.AnalyzeTicketAsync(ticket);

        // Assert
        Assert.Equal("Integration", result.Category);
        Assert.Equal("High", result.Priority);
        Assert.Equal("Webhook API", result.Product);
        Assert.True(result.KnownIssue);
        Assert.Contains("HTTPS", result.KnownIssueSummary);
    }

    private static string GetApiProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

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

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<string> _responses;

    public FakeHttpMessageHandler(Queue<string> responses)
    {
        _responses = responses;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_responses.Count == 0)
        {
            throw new InvalidOperationException(
                "No fake HTTP responses remain.");
        }

        var responseBody = _responses.Dequeue();

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                responseBody,
                Encoding.UTF8,
                "application/json")
        };

        return Task.FromResult(response);
    }
}