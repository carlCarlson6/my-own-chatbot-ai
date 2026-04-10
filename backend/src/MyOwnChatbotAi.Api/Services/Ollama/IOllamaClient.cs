namespace MyOwnChatbotAi.Api.Services.Ollama;

public interface IOllamaClient
{
    Task<IReadOnlyList<string>> ListModelNamesAsync(CancellationToken ct = default);
    Task<string> ChatAsync(string model, IReadOnlyList<OllamaMessage> messages, CancellationToken ct = default);
}

public sealed record OllamaMessage(string Role, string Content);
