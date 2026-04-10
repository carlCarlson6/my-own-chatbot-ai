# Scaffolding Plan

## Goal

Create a minimal but clean foundation for a local-first chatbot app using:

- **Backend:** .NET + Microsoft Orleans
- **Frontend:** Vite + React + TypeScript
- **Validation/UI contracts:** Zod + OpenAPI
- **AI runtime:** Ollama

## Implementation order

### 1. Contract-first foundation
- Create and maintain `contracts/chatbot-api.openapi.yml` as the canonical API definition.
- Keep the first version limited to conversation creation, message sending, history loading, and model listing.

### 2. Backend scaffold
- Add a `backend/` solution with a Minimal API host.
- Configure Orleans for local development.
- Start with one conversation-focused grain and a thin Ollama integration layer.

### 3. Frontend scaffold
- Add a `frontend/` app with Vite, React, TypeScript, Tailwind, Zustand, and Zod.
- Build the minimal chat shell: message list, composer, and status/error UI.

### 4. End-to-end integration
- Connect the frontend send-message action to the backend endpoint.
- First verify with a stubbed response, then replace with a real Ollama-backed flow.

## MVP defaults

- local Ollama only
- one default model
- in-memory conversations
- no authentication
- no token streaming in the first pass

## Immediate next actions

1. Finalize the OpenAPI contract
2. Scaffold the backend solution and Orleans host
3. Scaffold the frontend app and basic chat shell
4. Verify real build/run commands and update `README.md`
