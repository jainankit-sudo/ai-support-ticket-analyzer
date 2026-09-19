using AiSupportTicketAnalyzer.Api.AI;
using AiSupportTicketAnalyzer.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace AiSupportTicketAnalyzer.Api.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly AiService _aiService;

    public TicketsController(AiService aiService)
    {
        _aiService = aiService;
    }

    [HttpPost]
    public IActionResult CreateTicket([FromBody] SupportTicket ticket)
    {
        return Ok(new
        {
            message = "Ticket received successfully",
            ticket
        });
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeTicket(
        [FromBody] SupportTicket ticket)
    {
        var analysis =
            await _aiService.AnalyzeTicketAsync(ticket);

        return Ok(analysis);
    }
}