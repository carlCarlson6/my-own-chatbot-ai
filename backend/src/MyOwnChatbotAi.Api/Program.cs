using Microsoft.Extensions.Options;
using MyOwnChatbotAi.Api.Features.Conversations;
using MyOwnChatbotAi.Api.Features.Models;
using MyOwnChatbotAi.Api.Services;
using MyOwnChatbotAi.Api.Services.Ollama;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConversationService, InMemoryConversationService>();

builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.AddHttpClient<IOllamaClient, OllamaHttpClient>()
    .ConfigureHttpClient((sp, client) =>
    {
        var opts = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
        client.BaseAddress = new Uri(opts.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
    });

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { service = "my-own-chatbot-ai-api", status = "ok" }));
app.MapConversationEndpoints();
app.MapModelEndpoints();

app.Run();

public partial class Program;
