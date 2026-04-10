# Scaffolding Plan

_Last updated: 2026-04-10_

## Goal

Create a minimal but clean foundation for a local-first chatbot app using:

- **Backend:** .NET + Microsoft Orleans
- **Frontend:** Vite + React + TypeScript
- **Validation/UI contracts:** Zod + OpenAPI
- **AI runtime:** Ollama

## Pre-Implementation Change Review

Before continuing work from this plan, first confirm the repo has not changed since this document was last updated.

### Required review steps

1. Inspect `README.md`, `docs/`, `contracts/`, and the relevant `backend/` or `frontend/` folders.
2. Compare the current repo state against this plan and check for drift in structure, commands, status markers, or dependencies.
3. Update this plan first if any step is stale, already completed, or needs reordering.
4. Only after that review should implementation continue.

## Implementation order

### 1. Contract-first foundation ✅ Done
- Create and maintain `contracts/chatbot-api.openapi.yml` as the canonical API definition.
- Keep the first version limited to conversation creation, message sending, history loading, and model listing.

### 2. Backend scaffold ✅ Minimal API stub done
- The `backend/` solution and Minimal API host are now checked in.
- Conversation and model endpoints are organized by feature and currently delegate to `InMemoryConversationService`.
- Orleans hosting and the real Ollama integration layer are still pending for the next backend pass.

### 3. Frontend scaffold ✅ Done
- `frontend/` is checked in with **Vite + React + TypeScript + Tailwind CSS + Zustand + Zod**.
- Verified commands:
  - `npm run dev` — starts the Vite dev server
  - `npm run build` — type-checks with `tsc -b` then produces a production bundle
  - `npm run lint` — ESLint passes with zero warnings

### 4. Backend Ollama client layer ✅ Done
- `IOllamaClient`, `OllamaHttpClient`, `OllamaOptions`, and `OllamaException` are implemented and registered in `Program.cs`.
- `appsettings.json` holds the default Ollama config (base URL, default model, allowlist, timeout).
- `ListModelsEndpoint` calls Ollama via `IOllamaClient` and falls back to the configured allowlist when Ollama is unavailable.
- The in-memory conversation service is still used for `send` and `create` flows.

### 5. End-to-end integration ✅ Backend Orleans + Ollama wired
- Orleans silo (`UseLocalhostClustering` + `AddMemoryGrainStorage("conversations")`) is running in `Program.cs`.
- `IConversationGrain` / `ConversationGrain` own conversation state and delegate message generation to `IOllamaClient`.
- `SendMessageEndpoint`, `CreateConversationEndpoint`, and `GetConversationHistoryEndpoint` now delegate to the grain via `IGrainFactory`.
- Max message size (8,000 chars) validated in `SendMessageEndpoint`; clean `ApiError` payloads returned for Ollama failures.
- `POST /api/conversations/send` now returns a real Ollama-backed reply when Ollama is running.
- See `docs/backend-ollama-communication-plan.md` for full details.
- Frontend wiring to the live backend API is tracked separately in `docs/frontend-chat-ui-plan.md`.

## MVP defaults

- local Ollama only
- one default model
- in-memory conversations
- no authentication
- no token streaming in the first pass

## Immediate next actions

1. [x] Finalize the OpenAPI contract
2. [x] Scaffold the backend solution and Minimal API stub
3. [x] Scaffold the frontend app and verify build/run commands
4. [x] Add the backend-only Ollama client layer (`IOllamaClient`, `OllamaHttpClient`, `OllamaOptions`)
5. [x] Add Orleans hosting and conversation grain orchestration (see `docs/backend-ollama-communication-plan.md`)
6. [x] Frontend chat UI implementation tracked in `docs/frontend-chat-ui-plan.md`

All immediate next actions are complete. This scaffolding plan is done — further implementation work is tracked in dedicated plans (`docs/frontend-chat-ui-plan.md`, `docs/backend-ollama-communication-plan.md`).
