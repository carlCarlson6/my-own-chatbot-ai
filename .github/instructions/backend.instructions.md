---
description: "Use when working on the backend .NET API for the chatbot app. Covers Minimal APIs, vertical slice architecture, Orleans-based business logic, Ollama integration, and conversation state handling."
name: "Backend Chatbot API Guidelines"
applyTo: "**/*.{cs,json}"
---
# Backend Chatbot API Guidelines

## Architecture

- Build the backend as a **.NET API** that serves the frontend chat app.
- Use a **vertical slice architecture**: organize code by feature or use case, not by technical layer.
- Keep each slice self-contained with its endpoint, request/response models, validation, and Orleans interaction.

## API Style

- Use **Minimal APIs** rather than MVC controllers.
- Define each endpoint in a **separate file** to keep routes easy to discover and maintain.
- Keep endpoint mapping thin: request parsing, validation, delegation to the relevant application flow, and response shaping only.

## Orleans and Business Logic

- Use **Microsoft Orleans** for business logic orchestration and conversation state management.
- Put concurrency-sensitive chat workflows into grains to avoid race conditions and simplify stateful interactions.
- Keep endpoint files free of business logic; delegate conversation handling, message processing, and state tracking to Orleans grains/services.

## Ollama Integration

- The backend is the integration boundary between the frontend and **Ollama**.
- Endpoints should accept chat messages from the UI, send them to the Ollama model through a dedicated integration layer, and return validated responses.
- Isolate Ollama-specific client code so model-provider details do not leak into API endpoint definitions.

## Conversation Handling

- Persist and manage conversation context through Orleans-friendly abstractions.
- Use explicit request and response contracts for operations such as sending a message, loading a conversation, and listing conversation history.
- Design for clear loading, error, and retry behavior at the API boundary.

## Project Structure

- Prefer folders grouped by feature, for example `Conversations/SendMessage`, `Conversations/GetHistory`, or `Models/ListAvailableModels`.
- Inside each feature, keep related endpoint definitions and DTOs together.
- Avoid large shared service layers unless the reuse is real and stable.

## Validation and Contracts

- Keep API contracts explicit and frontend-friendly.
- Validate input at the API boundary and return predictable error payloads.
- Maintain a clean contract so the React frontend can pair easily with Zod schemas.

## Verification Before Completion

- After backend changes, run the relevant **restore**, **build**, and **test** commands once the .NET project files exist.
- Do not claim success unless those commands have been run and their output confirms success.
- Keep `README.md` updated with verified backend setup and run commands as the project grows.

## Repo Awareness

- This repository is still in an early scaffold stage. Verify actual folders, project files, and Orleans setup before assuming structure or commands.
- Keep changes minimal and aligned with the documented stack and integration boundaries.

## Examples

### Example Endpoint File Structure

Code structure:
```Backend/
  Conversations/
    SendMessage/
      SendMessageEndpoint.cs
      SendMessageRequest.cs
      SendMessageResponse.cs
```

SendMessageEndpoint.cs:
```csharp[ApiController]
public static class SendMessageEndpoint
{
    private const string Route = "/api/conversations/send";

    public static void MapSendMessageEndpoint(this WebApplication app)
    {
        app.MapPost(Route, async (SendMessageRequest request, IConversationService conversationService) =>
        {
            // Validate request, delegate to Orleans grain, and return response
            var response = await conversationService.SendMessageAsync(request);
            return Results.Ok(response);
        });
    }

    private static async Task<IResult> Handle(
        [FromServices] IConversationService conversationService,
        [FromBody] SendMessageRequest request)
    {
      .... ENDPOINT IMPLEMENTATION GOES HERE ...
    }
}
```
