using AiSupportTicketAnalyzer.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace AiSupportTicketAnalyzer.Tests;

public class KnownIssueRepositoryTests
{
    [Fact]
    public async Task SearchAsync_ReturnsMatchingKnownIssue()
    {
        // Arrange
        var environment = new TestWebHostEnvironment
        {
            ContentRootPath = GetApiProjectRoot()
        };

        var repository = new KnownIssueRepository(environment);

        // Act
        var result = await repository.SearchAsync("Webhook creation");

        // Assert
        Assert.Contains("KI-001", result);
        Assert.Contains("Webhook creation returns HTTP 500", result);
    }

    [Fact]
    public async Task SearchAsync_ReturnsNoMatchMessage_WhenIssueDoesNotExist()
    {
        // Arrange
        var environment = new TestWebHostEnvironment
        {
            ContentRootPath = GetApiProjectRoot()
        };

        var repository = new KnownIssueRepository(environment);

        // Act
        var result = await repository.SearchAsync("Something that does not exist");

        // Assert
        Assert.Equal(
            "No matching known issues were found.",
            result);
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

public class TestWebHostEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = string.Empty;

    public string EnvironmentName { get; set; } = "Development";

    public string ContentRootPath { get; set; } = string.Empty;

    public IFileProvider ContentRootFileProvider { get; set; } =
        new NullFileProvider();

    public string WebRootPath { get; set; } = string.Empty;

    public IFileProvider WebRootFileProvider { get; set; } =
        new NullFileProvider();
}