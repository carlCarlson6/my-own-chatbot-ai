# my-own-chatbot-ai

My own chatbot web app.

## Current Repo State

- The public API contract lives in `contracts/chatbot-api.openapi.yml` and is the source of truth.
- The checked-in implementation is a **.NET 8 Minimal API** backend under `backend/src/MyOwnChatbotAi.Api`.
- **Conversation endpoints** (`create`, `send`, `history`) are now orchestrated by **Microsoft Orleans** (`ConversationGrain`) which delegates message generation to `IOllamaClient`.
- **Model listing** (`GET /api/models`) calls **Ollama** directly via the backend client layer, with an automatic fallback to the configured allowlist when Ollama is unavailable.
- A frontend app is scaffolded under `frontend/` using **Vite + React + TypeScript + Tailwind CSS + Zustand + Zod**.
- Infrastructure is fully configured under `infrastructure/` — Docker (standalone + Compose) and Kubernetes manifests for all three services.

## Technologies

### Implemented

- **.NET 8** Minimal APIs (vertical slice architecture)
- **OpenAPI 3.0** contract-first API design
- **Ollama client layer** — `IOllamaClient` / `OllamaHttpClient` / `OllamaOptions` registered in the backend; model listing calls Ollama with allowlist-based fallback
- **Microsoft Orleans** — `ConversationGrain` owns conversation state and orchestrates Ollama-backed message generation
- **Vite + React + TypeScript** frontend with Tailwind CSS, Zustand, and Zod
- **Docker / Docker Compose** for containerised local and production-like runs
- **Kubernetes** manifests targeting the `chatbot-ai` namespace

### Planned next layers

- Wire the frontend send-message flow to the live backend API and verify full end-to-end chat.
- Token streaming support (deferred from MVP).

## Repo Structure

```
my-own-chatbot-ai/
├── backend/          .NET 8 Minimal API + Orleans (planned)
├── contracts/        OpenAPI YAML — source of truth for the API contract
├── docs/             Architecture and workflow planning documents
├── frontend/         Vite + React + TypeScript SPA
├── infrastructure/   Docker, Docker Compose, and Kubernetes configuration
└── .github/          Copilot instructions and reusable prompts
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
| Build frontend | `cd frontend && npm run build` | Produces `frontend/dist/` |
| Lint frontend | `cd frontend && npm run lint` | ESLint passes |
| Validate Compose | `cd infrastructure && docker compose -f docker-compose.yml config` | Prints resolved config with no errors |

## Architecture Boundaries

- `contracts/` defines the published transport contract for backend and frontend work.
- `backend/` contains the Minimal API implementation and the stubbed conversation flow.
- `frontend/` calls only the backend API; it never talks to Ollama directly.
- `infrastructure/` owns all Docker and Kubernetes configuration; no Dockerfiles live elsewhere.
- Ollama is a backend-only integration boundary.

## Plans

### WIP / In-Progress

Plans that are partially done or still being executed.

- [`docs/scaffolding-plan.md`](docs/scaffolding-plan.md) — Full-stack scaffolding: contract, backend, frontend, and Ollama + Orleans integration (step 6 — frontend wiring to live backend — still pending).
- [`docs/backend-ollama-communication-plan.md`](docs/backend-ollama-communication-plan.md) — Backend Ollama + Orleans communication: Phases 1–4 ✅ done; Phase 5 verification checklist still pending.
- [`docs/frontend-chat-ui-plan.md`](docs/frontend-chat-ui-plan.md) — Frontend chat UI implementation plan

### Completed

Plans that have been fully executed and are kept for historical reference.

- [`docs/old-plans/copilot-workflow-improvement-plan.md`](docs/old-plans/copilot-workflow-improvement-plan.md) — Copilot workflow improvement plan: dev-setup instructions, testing guidelines, chatbot-builder agent, and prompt cleanup.

## Next Steps

1. Wire the frontend to the live backend API and verify full end-to-end chat.
2. Add token streaming support (deferred from MVP).
