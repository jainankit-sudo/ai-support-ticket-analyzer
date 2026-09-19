namespace AiSupportTicketAnalyzer.Api.AI;

public class OllamaTool
{
    public string Type { get; set; } = string.Empty;
    public OllamaFunction Function { get; set; } = new();
}

public class OllamaFunction
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public OllamaFunctionParameters Parameters { get; set; } = new();
}

public class OllamaFunctionParameters
{
    public string Type { get; set; } = "object";
    public Dictionary<string, OllamaProperty> Properties { get; set; } = new();
    public List<string> Required { get; set; } = new();
}

public class OllamaProperty
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class OllamaToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public OllamaFunctionCall Function { get; set; } = new();
}

public class OllamaFunctionCall
{
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
}

public class OllamaMessage
{
    public string Role { get; set; } = string.Empty;
    public string? Content { get; set; }
    public List<OllamaToolCall>? ToolCalls { get; set; }

    public string? ToolCallId { get; set; }
}

public class OllamaResponse
{
    public OllamaResponseChoice[] Choices { get; set; } = Array.Empty<OllamaResponseChoice>();
}

public class OllamaResponseChoice
{
    public OllamaMessage Message { get; set; } = new();
}