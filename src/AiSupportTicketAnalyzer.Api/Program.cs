using AiSupportTicketAnalyzer.Api.AI;
using AiSupportTicketAnalyzer.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient<AiService>();
builder.Services.AddSingleton<KnownIssueRepository>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS disabled for local development for now.
// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();