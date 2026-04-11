namespace MyOwnChatbotAi.Api.Services.Ollama;

public interface IOllamaClient
{
    Task<IReadOnlyList<string>> ListModelNamesAsync(CancellationToken ct = default);
    Task<string> ChatAsync(string model, IReadOnlyList<OllamaMessage> messages, CancellationToken ct = default);
    IAsyncEnumerable<string> StreamChatAsync(
        string model,
        IReadOnlyList<OllamaMessage> messages,
        CancellationToken ct = default);
}

[GenerateSerializer]
public sealed record OllamaMessage(
    [property: Id(0)] string Role,
    [property: Id(1)] string Content);
