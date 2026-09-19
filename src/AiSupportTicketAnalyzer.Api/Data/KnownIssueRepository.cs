using System.Text.Json;

namespace AiSupportTicketAnalyzer.Api.Data;

public class KnownIssueRepository
{
    private readonly IWebHostEnvironment _environment;

    public KnownIssueRepository(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SearchAsync(string searchTerm)
    {
        var filePath = Path.Combine(
            _environment.ContentRootPath,
            "data",
            "known-issues.json");

        if (!File.Exists(filePath))
        {
            return "No known issues database was found.";
        }

        var json = await File.ReadAllTextAsync(filePath);

        var issues = JsonSerializer.Deserialize<List<KnownIssue>>(
            json,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true
            }) ?? new List<KnownIssue>();

        var matches = issues
            .Where(issue =>
                issue.Title.Contains(
                    searchTerm,
                    StringComparison.OrdinalIgnoreCase)
                ||
                issue.Description.Contains(
                    searchTerm,
                    StringComparison.OrdinalIgnoreCase)
                ||
                issue.Product.Contains(
                    searchTerm,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            return "No matching known issues were found.";
        }

        return JsonSerializer.Serialize(matches);
    }
}

public class KnownIssue
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Product { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Resolution { get; set; } = string.Empty;
}