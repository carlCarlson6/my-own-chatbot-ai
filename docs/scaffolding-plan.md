# Scaffolding Plan

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

### 4. End-to-end integration ⏳ Pending
- Connect the frontend send-message action to the backend endpoint.
- Replace the current stubbed assistant response with a real Orleans + Ollama flow.
- See `docs/backend-ollama-communication-plan.md` for the detailed execution plan.

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
4. [ ] Add Orleans hosting and backend-only Ollama integration (see `docs/backend-ollama-communication-plan.md`)
5. [ ] Wire the frontend send-message flow to the real backend + Ollama
