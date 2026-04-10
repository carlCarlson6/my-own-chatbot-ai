# my-own-chatbot-ai

My own chatbot web app.

## Current Repo State

- The public API contract lives in `contracts/chatbot-api.openapi.yml` and is the source of truth.
- The checked-in implementation is currently a **.NET 8 Minimal API** backend under `backend/src/MyOwnChatbotAi.Api`.
- Conversation and model endpoints are wired to an **in-memory stub service** for local development.
- A frontend app is scaffolded under `frontend/` using **Vite + React + TypeScript + Tailwind CSS + Zustand + Zod**.
- Infrastructure is fully configured under `infrastructure/` — Docker (standalone + Compose) and Kubernetes manifests for all three services.
- **Orleans** orchestration and **Ollama** integration are planned next steps rather than active runtime dependencies.

## Technologies

### Implemented

- **.NET 8** Minimal APIs
- **OpenAPI 3.0** contract-first API design
- **Vite + React + TypeScript** frontend with Tailwind CSS, Zustand, and Zod
- **Docker / Docker Compose** for containerised local and production-like runs
- **Kubernetes** manifests targeting the `chatbot-ai` namespace

### Planned next layers

- **Microsoft Orleans** for conversation orchestration and state ownership
- **Ollama** for local model inference

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
| Smoke check models | `curl http://localhost:5050/api/models` | Returns the stubbed `llama3.1` and `mistral` model list |
| Build frontend | `cd frontend && npm run build` | Produces `frontend/dist/` |
| Lint frontend | `cd frontend && npm run lint` | ESLint passes |
| Validate Compose | `cd infrastructure && docker compose -f docker-compose.yml config` | Prints resolved config with no errors |

## Architecture Boundaries

- `contracts/` defines the published transport contract for backend and frontend work.
- `backend/` contains the Minimal API implementation and the stubbed conversation flow.
- `frontend/` calls only the backend API; it never talks to Ollama directly.
- `infrastructure/` owns all Docker and Kubernetes configuration; no Dockerfiles live elsewhere.
- Ollama is a backend-only integration boundary.

## Next Steps

1. Replace the in-memory conversation stub with Orleans-backed orchestration.
2. Add the backend-only Ollama client and real model/message flows.
3. Wire the frontend to the live backend API and verify full end-to-end chat.
