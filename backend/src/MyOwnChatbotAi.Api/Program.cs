using MyOwnChatbotAi.Api.Features.Conversations;
using MyOwnChatbotAi.Api.Features.Models;
using MyOwnChatbotAi.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConversationService, InMemoryConversationService>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { service = "my-own-chatbot-ai-api", status = "ok" }));
app.MapConversationEndpoints();
app.MapModelEndpoints();

app.Run();

public partial class Program;
