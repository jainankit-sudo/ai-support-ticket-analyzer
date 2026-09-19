namespace AiSupportTicketAnalyzer.Api.Models;

public class TicketAnalysis
{
    public string Category { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public string Product { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public string CustomerImpact { get; set; } = string.Empty;

    public bool KnownIssue { get; set; }

    public string KnownIssueSummary { get; set; } = string.Empty;

    public string SuggestedResponse { get; set; } = string.Empty;
}