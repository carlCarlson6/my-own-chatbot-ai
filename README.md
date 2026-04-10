# my-own-chatbot-ai

My own chatbot web app.

## Current Repo State

- The public API contract lives in `contracts/chatbot-api.openapi.yml` and is the source of truth.
- The checked-in implementation is currently a **.NET 8 Minimal API** backend under `backend/src/MyOwnChatbotAi.Api`.
- Conversation and model endpoints are wired to an **in-memory stub service** for local development.
- A frontend app is scaffolded under `frontend/` using **Vite + React + TypeScript + Tailwind CSS + Zustand + Zod**.
- **Orleans** orchestration and **Ollama** integration are still planned next steps rather than active runtime dependencies.

## Technologies

### Implemented now
- **.NET 8** Minimal APIs
- **OpenAPI 3.0** contract-first API design
- **Vite + React + TypeScript** frontend scaffold with Tailwind CSS, Zustand, and Zod

### Planned next layers
- **Microsoft Orleans** for conversation orchestration and state ownership
- **Vite** + **React** + **TypeScript** frontend
- **Tailwind CSS**, **Zustand**, and **Zod**
- **Ollama** for local model inference

## Verified Commands

| Purpose | Command | Observed result |
| --- | --- | --- |
| Build backend | `dotnet build backend/src/MyOwnChatbotAi.sln` | Succeeds with `0` warnings and `0` errors |
| Run API | `dotnet run --project backend/src/MyOwnChatbotAi.Api` | Starts the API on `http://localhost:5050` |
| Smoke check root | `curl http://localhost:5050/` | Returns `{"service":"my-own-chatbot-ai-api","status":"ok"}` |
| Smoke check models | `curl http://localhost:5050/api/models` | Returns the stubbed `llama3.1` and `mistral` model list |

> There is no frontend `package.json` in the repo yet, so no verified frontend commands are documented.
> The current solution also does not include a dedicated test project.

## Architecture Boundaries

- `contracts/` defines the published transport contract for backend and future frontend work.
- `backend/` contains the current Minimal API implementation and the stubbed conversation flow.
- The future `frontend/` app should call only the backend API.
- Ollama should remain a backend-only integration boundary when it is added.

## Next Steps

1. Replace the in-memory conversation stub with Orleans-backed orchestration.
2. Add the backend-only Ollama client and real model/message flows.
3. Scaffold the `Vite + React + TypeScript` frontend and then verify its real run/build commands.
