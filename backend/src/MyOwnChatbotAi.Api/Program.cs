using MyOwnChatbotAi.Api.Authentication;
using Microsoft.Extensions.Options;
using MyOwnChatbotAi.Api.Features.Conversations;
using MyOwnChatbotAi.Api.Features.Conversations.Persistence;
using MyOwnChatbotAi.Api.Services.Ollama;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddClerkAuthenticationFoundation(builder.Configuration);
builder.Services.AddConversationPersistence(builder.Configuration);

builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.AddHttpClient<IOllamaClient, OllamaHttpClient>()
    .ConfigureHttpClient((sp, client) =>
    {
        var opts = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
        client.BaseAddress = new Uri(opts.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
    });

builder.Host.UseOrleans(silo =>
{
    silo.UseLocalhostClustering();
    silo.AddMemoryGrainStorage("conversations");
});

var app = builder.Build();

app.RegisterClerkAuthenticationResourceCleanup();

app.UseAuthentication();
app.UseClerkBearerValidation();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { service = "my-own-chatbot-ai-api", status = "ok" }));
app.MapConversationEndpoints();

app.Run();

public partial class Program;
