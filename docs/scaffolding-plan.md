# Scaffolding Plan

## Goal

Create a minimal but clean foundation for a local-first chatbot app using:

- **Backend:** .NET + Microsoft Orleans
- **Frontend:** Vite + React + TypeScript
- **Validation/UI contracts:** Zod + OpenAPI
- **AI runtime:** Ollama

## Implementation order

### 1. Contract-first foundation ✅ Done
- Create and maintain `contracts/chatbot-api.openapi.yml` as the canonical API definition.
- Keep the first version limited to conversation creation, message sending, history loading, and model listing.

### 2. Backend scaffold ✅ Minimal API stub done
- The `backend/` solution and Minimal API host are now checked in.
- Conversation and model endpoints are organized by feature and currently delegate to `InMemoryConversationService`.
- Orleans hosting and the real Ollama integration layer are still pending for the next backend pass.

### 3. Frontend scaffold ⏳ Pending
- A `frontend/` app has not been checked into the repo yet.
- When added, it should use Vite, React, TypeScript, Tailwind, Zustand, and Zod.
- Verify real frontend commands only after the frontend files and scripts exist.

### 4. End-to-end integration ⏳ Pending
- Connect the future frontend send-message action to the backend endpoint.
- Replace the current stubbed assistant response with a real Orleans + Ollama flow.

## MVP defaults

- local Ollama only
- one default model
- in-memory conversations
- no authentication
- no token streaming in the first pass

## Immediate next actions

1. [x] Finalize the OpenAPI contract
2. [x] Scaffold the backend solution and Minimal API stub
3. [ ] Resolve the local `npm`/Node issue and continue the frontend scaffold
4. [ ] Add Orleans hosting and backend-only Ollama integration
5. [ ] Scaffold the frontend app and basic chat shell
6. [ ] Verify real frontend build/run commands and update `README.md`
