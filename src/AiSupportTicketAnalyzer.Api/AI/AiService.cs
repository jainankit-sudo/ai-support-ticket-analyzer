using System.Text;
using System.Text.Json;
using AiSupportTicketAnalyzer.Api.Data;
using AiSupportTicketAnalyzer.Api.Models;

namespace AiSupportTicketAnalyzer.Api.AI;

public class AiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly KnownIssueRepository _knownIssueRepository;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public AiService(
        HttpClient httpClient,
        IConfiguration configuration,
        KnownIssueRepository knownIssueRepository)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _knownIssueRepository = knownIssueRepository;
    }

    public async Task<TicketAnalysis> AnalyzeTicketAsync(
        SupportTicket ticket)
    {
        var baseUrl = _configuration["AI:BaseUrl"];
        var model = _configuration["AI:Model"];

        var messages = new List<OllamaMessage>
        {
            new()
            {
                Role = "system",
                Content = """
                    You are a customer support ticket analysis assistant.

                    You have access to a tool called SearchKnownIssues.

                    When analyzing a ticket, search the known issues database
                    if the ticket might correspond to an existing problem.

                    After receiving the tool result, use it to produce the
                    final ticket analysis.

                    Return ONLY valid JSON with these properties:

                    category
                    priority
                    product
                    errorMessage
                    customerImpact
                    knownIssue
                    knownIssueSummary
                    suggestedResponse

                    knownIssue MUST be a JSON boolean.

                    priority must be one of:
                    Low, Medium, High, Critical.

                    Do not include markdown.
                    Do not include explanations outside the JSON.
                    """
            },
            new()
            {
                Role = "user",
                Content = $"""
                    Analyze this support ticket.

                    Title:
                    {ticket.Title}

                    Description:
                    {ticket.Description}

                    Product:
                    {ticket.Product}
                    """
            }
        };

        var tools = new List<OllamaTool>
        {
            new()
            {
                Type = "function",
                Function = new OllamaFunction
                {
                    Name = "SearchKnownIssues",
                    Description =
                        "Searches the company's known issues database for matching support issues.",
                    Parameters = new OllamaFunctionParameters
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OllamaProperty>
                        {
                            ["searchTerm"] = new OllamaProperty
                            {
                                Type = "string",
                                Description =
                                    "The issue, error message, or product to search for."
                            }
                        },
                        Required = new List<string>
                        {
                            "searchTerm"
                        }
                    }
                }
            }
        };

        while (true)
        {
            var requestBody = new
            {
                model,
                messages,
                tools,
                tool_choice = "auto",
                temperature = 0.1
            };

            var responseBody =
                await SendRequestAsync(
                    baseUrl!,
                    requestBody);

            var response =
                JsonSerializer.Deserialize<OllamaResponse>(
                    responseBody,
                    _jsonOptions)
                ?? throw new InvalidOperationException(
                    "AI returned an invalid response.");

            var message =
                response.Choices[0].Message;

            if (message.ToolCalls is { Count: > 0 })
            {
                var toolCall = message.ToolCalls[0];

                if (toolCall.Function.Name == "SearchKnownIssues")
                {
                    var arguments =
                        JsonSerializer.Deserialize<SearchKnownIssuesArguments>(
                            toolCall.Function.Arguments,
                            _jsonOptions);

                    var searchTerm =
                        arguments?.SearchTerm ?? string.Empty;

                    var toolResult =
                        await _knownIssueRepository.SearchAsync(
                            searchTerm);

                    messages.Add(message);

                    messages.Add(new OllamaMessage
                    {
                        Role = "tool",
                        ToolCallId = toolCall.Id,
                        Content = toolResult
                    });

                    continue;
                }
            }

            var content = message.Content;

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException(
                    "AI returned an empty response.");
            }

            try
            {
                return JsonSerializer.Deserialize<TicketAnalysis>(
                    content,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })
                    ?? throw new InvalidOperationException(
                        "AI response could not be converted to TicketAnalysis.");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"AI returned invalid TicketAnalysis JSON. AI response: {content}",
                    ex);
            }
        }
    }

    private async Task<string> SendRequestAsync(
        string baseUrl,
        object requestBody)
    {
        var json =
            JsonSerializer.Serialize(
                requestBody,
                _jsonOptions);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl.TrimEnd('/')}/chat/completions");

        request.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        using var response =
            await _httpClient.SendAsync(request);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"AI request failed: {response.StatusCode} - {responseBody}");
        }

        return responseBody;
    }
}

public class SearchKnownIssuesArguments
{
    public string SearchTerm { get; set; } = string.Empty;
}