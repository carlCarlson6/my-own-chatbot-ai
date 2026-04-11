# Post-MVP Features Plan

_Last updated: 2026-04-11_

## Goal

Evolve the working MVP chatbot into a polished, fully-featured local chatbot app by adding three capabilities in order: (1) token streaming for real-time assistant responses, (2) conversation sidebar for multi-session management, (3) test infrastructure for maintainability.

---

## Pre-Implementation Change Review

Before starting implementation work, first inspect the current repo state and confirm this plan still matches reality. If commands, structure, status, or acceptance criteria changed, refresh the plan before coding.

### Required review steps
1. Read `README.md`, `contracts/chatbot-api.openapi.yml`, and the affected implementation folders.
2. Compare the current repo against this plan: endpoints, DTOs, service registrations, status markers.
3. Update this plan first if any step is stale or already completed.
4. Only after that review should implementation continue.

---

## Current State (Verified 2026-04-10)

| Area | Status |
|---|---|
| Backend: create/send/history endpoints | ✅ Done |
| Backend: Orleans `ConversationGrain` | ✅ Done |
| Backend: Ollama client (non-streaming) | ✅ Done |
| Frontend: full chat UI (6 components + store) | ✅ Done |
| Infrastructure: Docker + Compose + Kubernetes | ✅ Done |
| Token streaming | ⏳ Pending |
| Conversation list/sidebar | ⏳ Pending |
| Test infrastructure | ⏳ Pending |

---

## Phase 1 — Token Streaming ⏳ Pending

Real-time streamed assistant responses via SSE (Server-Sent Events). Users see tokens appear incrementally instead of waiting for the full response.

### Strategy
The Ollama `/api/chat` endpoint supports `"stream": true` returning newline-delimited JSON objects. The backend will expose a new `POST /api/conversations/stream` endpoint that pipes Ollama's streaming response through as SSE. The `ConversationGrain` records the final assembled message after streaming completes.

### 1.1 — Contract update
- Add `POST /api/conversations/stream` to `contracts/chatbot-api.openapi.yml`
- Request body: same as `SendMessageRequest`
- Response: `text/event-stream` (SSE); each event contains a partial `content` chunk, final event contains `conversationId`, `userMessage`, `assistantMessage`, `model`, `status`
- Define `StreamChunkEvent` and `StreamCompleteEvent` schemas in the contract

### 1.2 — Backend: Ollama streaming client
- Add `ChatStreamAsync(string model, IReadOnlyList<OllamaMessage> messages, CancellationToken ct)` returning `IAsyncEnumerable<string>` to `IOllamaClient`
- Implement in `OllamaHttpClient` using `HttpCompletionOption.ResponseHeadersRead` + `ReadableStream`; parse each newline-delimited JSON chunk

### 1.3 — Backend: streaming endpoint
- Add `StreamMessageEndpoint.cs` in `Features/Conversations/StreamMessage/`
- Map `POST /api/conversations/stream` returning `text/event-stream`
- Call Ollama streaming directly from the endpoint handler
- After streaming completes, call `grain.RecordAssistantMessageAsync(...)` to persist both user + assembled assistant messages
- Add `RecordAssistantMessageAsync` to `IConversationGrain` and implement in `ConversationGrain`

### 1.4 — Frontend: streaming store + hook
- Add `streaming` to `ChatStatus` union in `chatStore.ts`
- Add `streamMessage(content: string): Promise<void>` action that uses `fetch` with a `ReadableStream` reader (EventSource-style via raw fetch since body is POST)
- Append a placeholder assistant `ChatMessage` to `messages` immediately, then update its `content` in place as chunks arrive
- On final event, replace placeholder with the real message ids + timestamps from the server

### 1.5 — Frontend: UI updates
- Update `MessageComposer` to call `streamMessage` instead of `sendMessage`
- Update `MessageList` / `MessageBubble` to handle partial/streaming messages (e.g. blinking cursor indicator)
- Fallback: keep `sendMessage` for backwards compatibility

---

## Phase 2 — Conversation Sidebar ⏳ Pending

List, browse, and switch between multiple conversations. The detailed execution plan for the Clerk-enabled version of this feature now lives in [`docs/plans/clerk-auth-multi-conversation-plan.md`](clerk-auth-multi-conversation-plan.md), because signed-in multi-conversation management and user-owned persistence are now treated as prerequisites while anonymous users remain on a single-chat path.

### 2.1 — Contract update
- Add `GET /api/conversations` to `contracts/chatbot-api.openapi.yml`
- Response: `ListConversationsResponse` with `conversations: ConversationSummary[]`
- Add `ConversationSummary` schema: `{ conversationId, title, model, status, createdAtUtc, lastMessageAt }`
- Update `CreateConversationResponse` and `SendMessageResponse` to ensure `title` and `createdAtUtc` are always returned (already in contract — verify)

### 2.2 — Backend: conversation registry grain
- Add `IConversationRegistryGrain` interface with `RegisterAsync(Guid id, string title, string model, DateTime createdAt)` and `ListAsync(): Task<IReadOnlyList<ConversationSummary>>`
- Implement `ConversationRegistryGrain` as a singleton grain (key = `Guid.Empty` or a well-known string) using `AddMemoryGrainStorage`
- Store a list of `ConversationSummaryState` records; add `[GenerateSerializer]` and `[Id]` attrs to all state types
- Call `registry.RegisterAsync(...)` inside `ConversationGrain.InitializeAsync()`

### 2.3 — Backend: list conversations endpoint
- Add `ListConversationsEndpoint.cs` in `Features/Conversations/ListConversations/`
- Map `GET /api/conversations` → resolve `IConversationRegistryGrain` → return list

### 2.4 — Frontend: multi-conversation store state
- Extend `chatStore.ts` (or split to `conversationListStore.ts`) to hold `conversations: ConversationSummary[]` and `activeConversationId`
- Add `loadConversations()` action calling `GET /api/conversations`
- Add `switchConversation(id)` action loading history via `GET /api/conversations/{id}/history` and setting active messages
- Add `newConversation()` action resetting to a fresh state

### 2.5 — Frontend: sidebar component
- Add `ConversationSidebar.tsx` — scrollable list of `ConversationSummary` items with title, model badge, and timestamp
- Add "New conversation" button at the top
- Highlight active conversation
- Update `ChatLayout.tsx` to render sidebar alongside the chat panel (collapsible on small screens)

---

## Phase 3 — Test Infrastructure ⏳ Pending

Add .NET integration tests and frontend unit tests to prevent regressions.

### 3.1 — Backend: test project setup
- Add `MyOwnChatbotAi.Api.Tests` xUnit project to the solution under `backend/tests/`
- Add `Microsoft.AspNetCore.Mvc.Testing` + `Orleans.TestingHost` + `FluentAssertions` NuGet packages
- Add a base `ApiTestFixture` using `WebApplicationFactory<Program>` that substitutes a mock `IOllamaClient`

### 3.2 — Backend: endpoint integration tests
- `Tests/Conversations/CreateConversationTests.cs` — POST /api/conversations: happy path, missing body defaults
- `Tests/Conversations/SendMessageTests.cs` — POST /api/conversations/send: valid request, empty message (400), Ollama error fallback
- `Tests/Conversations/GetConversationHistoryTests.cs` — GET history: 200 after messages, 404 for unknown id
- `Tests/Models/ListModelsTests.cs` — GET /api/models: returns models, fallback when Ollama unavailable

### 3.3 — Frontend: testing setup
- Add `vitest` + `@testing-library/react` + `@testing-library/user-event` + `msw` to `frontend/package.json`
- Configure `vite.config.ts` with test environment + jsdom
- Add a `src/test/` folder with `setup.ts` and `msw/handlers.ts` mocking the backend API

### 3.4 — Frontend: component + store tests
- `store/chatStore.test.ts` — `loadModels`, `sendMessage`, `streamMessage`, error states (uses MSW)
- `components/MessageComposer.test.tsx` — renders, sends on Enter, disabled while sending
- `components/MessageList.test.tsx` — renders messages, shows streaming indicator
- `components/ConversationSidebar.test.tsx` — lists conversations, fires switch callback

---

## Acceptance Criteria

### Phase 1 — Streaming
- [ ] `POST /api/conversations/stream` returns `text/event-stream` with incremental tokens
- [ ] Frontend shows tokens appearing word-by-word; no blank waiting period
- [ ] Completed message is persisted in the grain and retrievable via `GET /api/conversations/{id}/history`
- [ ] Fallback (non-streaming send) still works

### Phase 2 — Sidebar
- [ ] `GET /api/conversations` returns all conversations created in the current silo lifetime
- [ ] Sidebar lists conversations; clicking one loads its history into the chat panel
- [ ] "New conversation" clears the chat and starts fresh on the next message send
- [ ] Active conversation is visually highlighted

### Phase 3 — Tests
- [ ] `dotnet test` passes with ≥ 10 meaningful integration test cases
- [ ] `npm run test` (Vitest) passes with ≥ 10 meaningful component/store test cases
- [ ] `README.md` updated with verified `dotnet test` and `npm run test` commands
