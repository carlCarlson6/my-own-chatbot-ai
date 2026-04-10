using Microsoft.Extensions.Options;
using MyOwnChatbotAi.Api.Contracts;
using MyOwnChatbotAi.Api.Services.Ollama;

namespace MyOwnChatbotAi.Api.Features.Models;

public static class ListModelsEndpoint
{
    private const string Route = "/api/models";

    public static IEndpointRouteBuilder MapModelEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Route, Handle)
            .WithTags("Models")
            .WithName("ListModels")
            .Produces<ListModelsResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        IOllamaClient ollamaClient,
        IOptions<OllamaOptions> options,
        CancellationToken ct)
    {
        var opts = options.Value;
        var defaultModel = opts.DefaultModel;

        IReadOnlyList<string> available;
        try
        {
            available = await ollamaClient.ListModelNamesAsync(ct);
        }
        catch
        {
            // Ollama unavailable — fall back to the configured allowlist
            available = opts.AllowedModels;
        }

        // Normalise Ollama names (strip ":latest" suffix for matching)
        var availableNormalised = available
            .Select(n => n.Contains(':') ? n[..n.IndexOf(':')] : n)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var models = opts.AllowedModels
            .Where(m => availableNormalised.Contains(m))
            .Select(m => new ModelSummary(
                Name: m,
                DisplayName: m,
                IsDefault: string.Equals(m, defaultModel, StringComparison.OrdinalIgnoreCase),
                Description: null))
            .ToList();

        // If intersection is empty (e.g. Ollama is down and nothing matches),
        // return all allowed models as a graceful fallback
        if (models.Count == 0)
        {
            models = opts.AllowedModels
                .Select(m => new ModelSummary(
                    Name: m,
                    DisplayName: m,
                    IsDefault: string.Equals(m, defaultModel, StringComparison.OrdinalIgnoreCase),
                    Description: null))
                .ToList();
        }

        return Results.Ok(new ListModelsResponse(models));
    }
}

