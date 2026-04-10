using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyOwnChatbotAi.Api.Services.Ollama;

public sealed class OllamaHttpClient : IOllamaClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;

    public OllamaHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<string>> ListModelNamesAsync(CancellationToken ct = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync("/api/tags", ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new OllamaException("Failed to connect to Ollama to list models.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new OllamaException(
                $"Ollama returned {(int)response.StatusCode} when listing models.");
        }

        var tagsResponse = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(JsonOptions, ct)
            ?? throw new OllamaException("Ollama returned an empty response for /api/tags.");

        return tagsResponse.Models
            .Select(m => m.Name)
            .ToList()
            .AsReadOnly();
    }

    public async Task<string> ChatAsync(
        string model,
        IReadOnlyList<OllamaMessage> messages,
        CancellationToken ct = default)
    {
        var requestBody = new OllamaChatRequest(
            model,
            messages.Select(m => new OllamaChatMessage(m.Role, m.Content)).ToList(),
            Stream: false);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("/api/chat", requestBody, JsonOptions, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new OllamaException($"Failed to connect to Ollama for chat with model '{model}'.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new OllamaException(
                $"Ollama returned {(int)response.StatusCode} for chat with model '{model}'.");
        }

        var chatResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(JsonOptions, ct)
            ?? throw new OllamaException("Ollama returned an empty response for /api/chat.");

        return chatResponse.Message.Content;
    }

    // Internal JSON shape records — not exposed outside this file

    private sealed record OllamaTagsResponse(IReadOnlyList<OllamaModelEntry> Models);
    private sealed record OllamaModelEntry(string Name);

    private sealed record OllamaChatRequest(
        string Model,
        IReadOnlyList<OllamaChatMessage> Messages,
        bool Stream);

    private sealed record OllamaChatMessage(string Role, string Content);

    private sealed record OllamaChatResponse(OllamaChatMessage Message, bool Done);
}
