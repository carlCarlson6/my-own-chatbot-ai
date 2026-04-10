using MyOwnChatbotAi.Api.Contracts;
using MyOwnChatbotAi.Api.Services;

namespace MyOwnChatbotAi.Api.Features.Models;

public static class ListModelsEndpoint
{
    public static IEndpointRouteBuilder MapModelEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/models",
            (IConversationService conversations) => Results.Ok(conversations.GetModels()))
            .WithTags("Models")
            .WithName("ListModels")
            .Produces<ListModelsResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        return app;
    }
}
