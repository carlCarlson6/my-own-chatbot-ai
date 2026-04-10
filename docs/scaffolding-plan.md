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

### 2. Backend scaffold ✅ Done
- Add a `backend/` solution with a Minimal API host.
- Configure Orleans for local development.
- Start with one conversation-focused grain and a thin Ollama integration layer.

### 3. Frontend scaffold 🚧 In progress
- Add a `frontend/` app with Vite, React, TypeScript, Tailwind, Zustand, and Zod.
- Build the minimal chat shell: message list, composer, and status/error UI.
- **Current blocker:** local `npm`/Node tooling is failing, so this step is temporarily paused while we work on the fix and then continue.

### 4. End-to-end integration ⏳ Pending
- Connect the frontend send-message action to the backend endpoint.
- First verify with a stubbed response, then replace with a real Ollama-backed flow.

## MVP defaults

- local Ollama only
- one default model
- in-memory conversations
- no authentication
- no token streaming in the first pass

## Immediate next actions

1. [x] Finalize the OpenAPI contract
2. [x] Scaffold the backend solution and Orleans host
3. [ ] Resolve the local `npm`/Node issue and continue the frontend scaffold
4. [ ] Scaffold the frontend app and basic chat shell
5. [ ] Verify real frontend build/run commands and update `README.md`
