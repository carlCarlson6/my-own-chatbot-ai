using System.Net.Http.Json;
using System.Runtime.CompilerServices;
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
    private readonly ILogger<OllamaHttpClient> _logger;

    public OllamaHttpClient(HttpClient httpClient, ILogger<OllamaHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> ListModelNamesAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching model list from Ollama at {BaseAddress}", _httpClient.BaseAddress);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync("/api/tags", ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to connect to Ollama to list models");
            throw new OllamaException("Failed to connect to Ollama to list models.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Ollama returned {StatusCode} when listing models", (int)response.StatusCode);
            throw new OllamaException(
                $"Ollama returned {(int)response.StatusCode} when listing models.");
        }

        var tagsResponse = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(JsonOptions, ct)
            ?? throw new OllamaException("Ollama returned an empty response for /api/tags.");

        _logger.LogDebug("Ollama returned {ModelCount} available models", tagsResponse.Models.Count);

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
        _logger.LogDebug(
            "Sending chat request to Ollama: model='{Model}', messages={MessageCount}",
            model, messages.Count);

        var normalizedModel = NormalizeModelName(model);

        var requestBody = new OllamaChatRequest(
            normalizedModel,
            messages.Select(m => new OllamaChatMessage(m.Role, m.Content)).ToList(),
            Stream: false);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("/api/chat", requestBody, JsonOptions, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to connect to Ollama for chat with model '{Model}'", model);
            throw new OllamaException($"Failed to connect to Ollama for chat with model '{model}'.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Ollama returned {StatusCode} for chat with model '{Model}'",
                (int)response.StatusCode, model);
            throw new OllamaException(
                $"Ollama returned {(int)response.StatusCode} for chat with model '{model}'.");
        }

        var chatResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(JsonOptions, ct)
            ?? throw new OllamaException("Ollama returned an empty response for /api/chat.");

        _logger.LogDebug(
            "Ollama chat response received for model '{Model}': {ContentLength} characters",
            model, chatResponse.Message.Content.Length);

        return chatResponse.Message.Content;
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        string model,
        IReadOnlyList<OllamaMessage> messages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Sending streaming chat request to Ollama: model='{Model}', messages={MessageCount}",
            model, messages.Count);

        var normalizedModel = NormalizeModelName(model);
        var requestBody = new OllamaChatRequest(
            normalizedModel,
            messages.Select(m => new OllamaChatMessage(m.Role, m.Content)).ToList(),
            Stream: true);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(requestBody, options: JsonOptions)
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to connect to Ollama for streaming chat with model '{Model}'", model);
            throw new OllamaException($"Failed to connect to Ollama for streaming chat with model '{model}'.", ex);
        }

        using var streamingResponse = response;

        if (!streamingResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Ollama returned {StatusCode} for streaming chat with model '{Model}'",
                (int)streamingResponse.StatusCode, model);
            throw new OllamaException(
                $"Ollama returned {(int)streamingResponse.StatusCode} for streaming chat with model '{model}'.");
        }

        await using var responseStream = await streamingResponse.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(responseStream);

        var sawCompletionChunk = false;

        while (true)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            OllamaChatStreamResponse chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<OllamaChatStreamResponse>(line, JsonOptions)
                    ?? throw new OllamaException("Ollama returned an empty streaming chunk for /api/chat.");
            }
            catch (JsonException ex)
            {
                throw new OllamaException("Ollama returned an invalid streaming chunk for /api/chat.", ex);
            }

            if (!string.IsNullOrWhiteSpace(chunk.Error))
            {
                throw new OllamaException(chunk.Error);
            }

            if (!string.IsNullOrEmpty(chunk.Message?.Content))
            {
                yield return chunk.Message.Content;
            }

            if (chunk.Done)
            {
                sawCompletionChunk = true;
                break;
            }
        }

        if (!sawCompletionChunk)
        {
            throw new OllamaException("Ollama closed the response stream before completion.");
        }
    }

    private static string NormalizeModelName(string model) =>
        model.Contains(':') ? model : $"{model}:latest";

    // Internal JSON shape records — not exposed outside this file

    private sealed record OllamaTagsResponse(IReadOnlyList<OllamaModelEntry> Models);
    private sealed record OllamaModelEntry(string Name);

    private sealed record OllamaChatRequest(
        string Model,
        IReadOnlyList<OllamaChatMessage> Messages,
        bool Stream);

    private sealed record OllamaChatMessage(string Role, string Content);

    private sealed record OllamaChatResponse(OllamaChatMessage Message, bool Done);
    private sealed record OllamaChatStreamResponse(OllamaChatMessage? Message, bool Done, string? Error);
}
