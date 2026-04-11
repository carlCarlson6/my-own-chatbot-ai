using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using MyOwnChatbotAi.Api.Authentication;
using MyOwnChatbotAi.Api.Contracts;
using MyOwnChatbotAi.Api.Grains;
using MyOwnChatbotAi.Api.Services.Ollama;

namespace MyOwnChatbotAi.Api.Features.Conversations;

public static class StreamMessageEndpoint
{
    private const string Route = "/stream";
    private const int MaxMessageLength = 8_000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static RouteGroupBuilder MapStreamMessageEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost(Route, Handle)
            .WithName("StreamMessage")
            .Produces(StatusCodes.Status200OK, contentType: "text/event-stream")
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces<ApiError>(StatusCodes.Status404NotFound)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        return group;
    }

    private static async Task<IResult> Handle(
        SendMessageRequest request,
        HttpContext httpContext,
        IGrainFactory grains,
        ICurrentUser currentUser,
        IOllamaClient ollamaClient)
    {
        if (request.Message is null || string.IsNullOrWhiteSpace(request.Message.Content))
        {
            return Results.BadRequest(new ApiError("validation_error", "Message content is required.", "message"));
        }

        if (request.Message.Content.Length > MaxMessageLength)
        {
            return Results.BadRequest(new ApiError(
                "validation_error",
                $"Message content must not exceed {MaxMessageLength} characters.",
                "message"));
        }

        var conversationId = request.ConversationId ?? Guid.NewGuid();
        var grain = grains.GetGrain<IConversationGrain>(conversationId);

        StreamMessageStartResponse? streamStart;
        try
        {
            streamStart = await grain.BeginStreamMessageAsync(
                currentUser.UserId,
                request.Message,
                request.ConversationId is null);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(
                new ApiError("conversation_stream_error", ex.Message),
                statusCode: StatusCodes.Status500InternalServerError);
        }

        if (streamStart is null)
        {
            return Results.NotFound(
                new ApiError("conversation_not_found", $"Conversation '{conversationId}' was not found.", "conversationId"));
        }

        var requestAborted = httpContext.RequestAborted;
        var response = httpContext.Response;
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        response.Headers.Append("X-Accel-Buffering", "no");
        httpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        var startedAt = DateTime.UtcNow;
        var assistantContent = new StringBuilder();
        var sequence = 0;

        try
        {
            await WriteEventAsync(
                response,
                "started",
                new ConversationStreamStartedEvent(
                    "started",
                    streamStart.ConversationId,
                    streamStart.UserMessage,
                    streamStart.Conversation,
                    streamStart.Model,
                    streamStart.AssistantMessageId),
                requestAborted);

            await foreach (var delta in ollamaClient.StreamChatAsync(streamStart.Model, streamStart.OllamaMessages, requestAborted))
            {
                assistantContent.Append(delta);

                await WriteEventAsync(
                    response,
                    "chunk",
                    new ConversationStreamChunkEvent(
                        "chunk",
                        streamStart.ConversationId,
                        streamStart.AssistantMessageId,
                        delta,
                        sequence++),
                    requestAborted);
            }

            var completedResponse = await grain.CompleteStreamMessageAsync(
                currentUser.UserId,
                streamStart.UserMessage,
                streamStart.AssistantMessageId,
                assistantContent.ToString(),
                (int)(DateTime.UtcNow - startedAt).TotalMilliseconds);

            if (completedResponse is null)
            {
                throw new InvalidOperationException("Conversation could not be finalized after streaming completed.");
            }

            await WriteEventAsync(
                response,
                "completed",
                new ConversationStreamCompletedEvent(
                    "completed",
                    completedResponse.ConversationId,
                    completedResponse.UserMessage,
                    completedResponse.AssistantMessage,
                    completedResponse.Conversation,
                    completedResponse.Model,
                    completedResponse.Status,
                    completedResponse.LatencyMs),
                requestAborted);
        }
        catch (OperationCanceledException) when (requestAborted.IsCancellationRequested)
        {
            await grain.AbortStreamMessageAsync(currentUser.UserId, streamStart.AssistantMessageId);
        }
        catch (IOException)
        {
            await grain.AbortStreamMessageAsync(currentUser.UserId, streamStart.AssistantMessageId);
        }
        catch (OllamaException ex)
        {
            await grain.AbortStreamMessageAsync(currentUser.UserId, streamStart.AssistantMessageId);
            await TryWriteErrorEventAsync(
                response,
                streamStart,
                new ApiError("upstream_model_error", ex.Message),
                requestAborted);
        }
        catch (InvalidOperationException ex)
        {
            await grain.AbortStreamMessageAsync(currentUser.UserId, streamStart.AssistantMessageId);
            await TryWriteErrorEventAsync(
                response,
                streamStart,
                new ApiError("conversation_stream_error", ex.Message),
                requestAborted);
        }

        return Results.Empty;
    }

    private static async Task TryWriteErrorEventAsync(
        HttpResponse response,
        StreamMessageStartResponse streamStart,
        ApiError error,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await WriteEventAsync(
                response,
                "error",
                new ConversationStreamErrorEvent(
                    "error",
                    streamStart.ConversationId,
                    streamStart.AssistantMessageId,
                    error.Code,
                    error.Message,
                    error.Target,
                    error.Details),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (IOException)
        {
        }
    }

    private static async Task WriteEventAsync<T>(
        HttpResponse response,
        string eventName,
        T payload,
        CancellationToken cancellationToken)
    {
        await response.WriteAsync($"event: {eventName}\n", cancellationToken);
        await response.WriteAsync($"data: {JsonSerializer.Serialize(payload, JsonOptions)}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
