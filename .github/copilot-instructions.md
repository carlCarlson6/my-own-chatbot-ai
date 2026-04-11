# Copilot Instructions

## Project

`my-own-chatbot-ai` is a personal chatbot web app with a **contract-first scaffold**: `.NET 8` Minimal API backend in `backend/src/MyOwnChatbotAi.Api`, canonical API contract in `contracts/chatbot-api.openapi.yml`, and a Vite + React + TypeScript frontend under `frontend/`.

See `README.md` for the current verified repo summary and status.

## Stack

| Layer | Technology |
|---|---|
| Backend | .NET 8, Minimal APIs, Microsoft Orleans, Ollama client |
| Frontend | Vite + React + TypeScript, Tailwind CSS, Zustand, Zod |
| AI runtime | Ollama (local, `http://localhost:11434`) |
| Contracts | OpenAPI YAML in `contracts/chatbot-api.openapi.yml` |

## Verified Commands

| Task | Command |
|---|---|
| Backend build | `dotnet build backend/src/MyOwnChatbotAi.sln` |
| Backend run | `dotnet run --project backend/src/MyOwnChatbotAi.Api` (→ http://localhost:5050) |
| Frontend build | `cd frontend && npm run build` |
| Frontend lint | `cd frontend && npm run lint` |
| Frontend dev | `cd frontend && npm run dev` |

No dedicated test project exists yet. Before running any command, verify real project files exist (`.sln`, `.csproj`, `package.json`, `vite.config.*`).

## Git Commit Convention

**After completing any task**, stage all changed files, commit, and push:
- Imperative-mood message (e.g. `Add X`, `Fix Y`, `Update Z`)
- Always include the co-author trailer:
  ```
  Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
  ```
- Push with `git push` (or `git push -u origin <branch>` for new branches)

## Authoritative Guidance

All conventions, rules, and workflow assets live under `.github/` — especially `.github/instructions/`, `.github/agents/`, `.github/prompts/`, `.github/skills/`, and `.github/hooks/`. This file is project context only — do not duplicate guidance here.
