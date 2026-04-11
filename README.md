# my-own-chatbot-ai

My own chatbot web app.

## Current Repo State

- The public API contract lives in `contracts/chatbot-api.openapi.yml` and is the source of truth.
- The checked-in implementation is a **.NET 8 Minimal API** backend under `backend/src/MyOwnChatbotAi.Api`.
- **Conversation endpoints** (`create`, `send`, `history`) are now orchestrated by **Microsoft Orleans** (`ConversationGrain`) which delegates message generation to `IOllamaClient`.
- **Model listing** (`GET /api/models`) calls **Ollama** directly via the backend client layer, with an automatic fallback to the configured allowlist when Ollama is unavailable.
- A frontend chat UI is implemented under `frontend/` — native `fetch` + Zod API client layer, Zustand store, and React components (MessageList, MessageBubble, MessageComposer, ModelSelector, ConversationHeader, ChatLayout) are all wired to the backend. The Vite dev server proxies `/api/*` to `localhost:5050`.
- Infrastructure is fully configured under `infrastructure/` — Docker (standalone + Compose) and Kubernetes manifests for all three services.

## Technologies

### Implemented

- **.NET 8** Minimal APIs (vertical slice architecture)
- **OpenAPI 3.0** contract-first API design
- **Ollama client layer** — `IOllamaClient` / `OllamaHttpClient` / `OllamaOptions` registered in the backend; model listing calls Ollama with allowlist-based fallback
- **Microsoft Orleans** — `ConversationGrain` owns conversation state and orchestrates Ollama-backed message generation
- **Vite + React + TypeScript** frontend with Tailwind CSS, Zustand, and Zod — **fully wired to the backend API** with a native `fetch` client, Zod-validated responses, and a Zustand store driving the chat UI
- **Docker / Docker Compose** for containerised local and production-like runs
- **Kubernetes** manifests targeting the `chatbot-ai` namespace

### Planned next layers

- Token streaming support (deferred from MVP).
- Conversation history browser / sidebar (single conversation per session in MVP).

## Repo Structure

```
my-own-chatbot-ai/
├── backend/          .NET 8 Minimal API + Orleans
├── contracts/        OpenAPI YAML — source of truth for the API contract
├── docs/plans/            Architecture and workflow planning documents
├── frontend/         Vite + React + TypeScript SPA
├── infrastructure/   Docker, Docker Compose, and Kubernetes configuration
└── .github/          Copilot agents, instructions, prompts, and skills
    ├── agents/       3 custom Copilot coding agents
    ├── instructions/ 8 convention instruction files
    ├── prompts/      6 reusable prompt files
    └── skills/       5 on-demand skill files
```

## Infrastructure

All containerisation and deployment configuration lives in [`infrastructure/`](infrastructure/README.md).

### Services

| Service      | Technology           | Port (local) | Role                            |
|--------------|----------------------|:------------:|---------------------------------|
| **backend**  | .NET 8 Minimal API   | `5050`       | Chat API and business logic     |
| **frontend** | Vite + React + nginx | `3000`       | SPA served by nginx             |
| **ollama**   | Ollama               | `11434`      | Local LLM inference             |

```
Browser
  │
  ▼
frontend (nginx :3000 / :80)
  │ proxies /api/* ──────────────────────────────────▶ backend (:5050)
  │                                                          │
  │                                                          │ HTTP
  │                                                          ▼
  │                                                    ollama (:11434)
```

### Quick start (Docker Compose)

```bash
# Run the full stack (production-like)
cd infrastructure
docker compose -f docker-compose.yml up --build

# Run with hot reload for local development (override file auto-merged)
cd infrastructure
docker compose up --build
```

After startup: frontend → http://localhost:3000 · backend → http://localhost:5050 · ollama → http://localhost:11434

See [`infrastructure/README.md`](infrastructure/README.md) for standalone container commands, Kubernetes deployment, environment variables, GPU setup, and more.

## Verified Commands

| Purpose | Command | Observed result |
| --- | --- | --- |
| Build backend | `dotnet build backend/src/MyOwnChatbotAi.sln` | Succeeds with `0` warnings and `0` errors |
| Run API | `dotnet run --project backend/src/MyOwnChatbotAi.Api` | Starts the API on `http://localhost:5050` |
| Smoke check root | `curl http://localhost:5050/` | Returns `{"service":"my-own-chatbot-ai-api","status":"ok"}` |
| Smoke check models | `curl http://localhost:5050/api/models` | Returns models from Ollama (filtered by allowlist), or falls back to the configured `llama3.1` / `mistral` allowlist when Ollama is unreachable |
| Build frontend | `cd frontend && npm run build` | Produces `frontend/dist/` — zero TypeScript errors |
| Lint frontend | `cd frontend && npm run lint` | ESLint passes — zero errors |
| Dev frontend | `cd frontend && npm run dev` | Vite dev server on `http://localhost:5173`; `/api` proxied to `http://localhost:5050` |
| Validate Compose | `cd infrastructure && docker compose -f docker-compose.yml config` | Prints resolved config with no errors |

## Architecture Boundaries

- `contracts/` defines the published transport contract for backend and frontend work.
- `backend/` contains the Minimal API implementation and the stubbed conversation flow.
- `frontend/` calls only the backend API; it never talks to Ollama directly.
- `infrastructure/` owns all Docker and Kubernetes configuration; no Dockerfiles live elsewhere.
- Ollama is a backend-only integration boundary.

## Plans

### WIP / In-Progress

- [`docs/plans/post-mvp-features-plan.md`](docs/plans/post-mvp-features-plan.md) — Post-MVP features: token streaming, conversation sidebar, and test infrastructure.

### Completed

Plans that have been fully executed and are kept for historical reference.

- [`docs/plans/old/frontend-chat-ui-plan.md`](docs/plans/old/frontend-chat-ui-plan.md) — Frontend chat UI: API client (native fetch + Zod), Zustand store, 6 React components, end-to-end wiring, and UX polish. All 5 phases ✅ done.
- [`docs/plans/old/backend-ollama-communication-plan.md`](docs/plans/old/backend-ollama-communication-plan.md) — Backend Ollama + Orleans communication: all 5 phases ✅ done.
- [`docs/plans/old/scaffolding-plan.md`](docs/plans/old/scaffolding-plan.md) — Full-stack scaffolding: contract, backend, frontend, and Ollama + Orleans integration. All 6 steps complete; frontend wiring tracked in `docs/plans/frontend-chat-ui-plan.md`.
- [`docs/plans/old/copilot-workflow-improvement-plan.md`](docs/plans/old/copilot-workflow-improvement-plan.md) — Copilot workflow improvement plan: dev-setup instructions, testing guidelines, osmany-development agent, and prompt cleanup.

## Documentation

| Guide | Description |
|-------|-------------|
| [`docs/quickstart.md`](docs/quickstart.md) | Mac-native quickstart: prerequisites, Docker Compose setup, local dev, port reference, and useful commands |

## Next Steps

1. Add token streaming support (deferred from MVP).
2. Add conversation history browser / sidebar.
3. Add test infrastructure (unit + integration tests for backend slices and frontend store).
