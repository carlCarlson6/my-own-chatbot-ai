# Copilot Instructions

## Project

`my-own-chatbot-ai` is a personal chatbot web app. The repository now includes a **contract-first scaffold** with a checked-in `.NET 8` Minimal API backend in `backend/src/MyOwnChatbotAi.Api` and the canonical API contract in `contracts/chatbot-api.openapi.yml`.

There is no checked-in frontend app yet, and the current backend still uses an in-memory stub for chat flows. Treat the Orleans, React, and Ollama notes below as the planned direction for the next implementation steps.

See `README.md` for the current verified repo summary.

## Planned Architecture

- **Backend**: .NET with **Microsoft Orleans** for actor-style chat orchestration
- **Frontend**: **Vite + React + TypeScript** with Tailwind CSS, Zustand, and Zod
- **AI runtime**: **Ollama** for local LLM inference

Keep frontend and backend concerns clearly separated, and document integration boundaries when adding new pieces.

## Build and Test

- Verified backend commands:
  - `dotnet build backend/src/MyOwnChatbotAi.sln`
  - `dotnet run --project backend/src/MyOwnChatbotAi.Api`
- Verified development URL: `http://localhost:5050`
- There is currently **no frontend `package.json`** and **no dedicated test project** checked into the repo.
- Before suggesting additional commands, still check for real project files such as `package.json`, `.sln`, `.csproj`, or `vite.config.*`.
- Keep `README.md` and this file in sync as more verified commands are added.

## Planning Documents

- When creating or updating an implementation, scaffolding, or workflow plan in `docs/`, include a `## Pre-Implementation Change Review` section near the top.
- That section should instruct the next agent to inspect the latest repo state, compare it against the plan, update any stale steps or statuses, and only then begin implementation.
- Treat this as a required review gate whenever repo changes may have happened since the plan was last updated.

## Conventions

- Keep changes minimal and aligned with the documented stack.
- Do **not** assume folders, scripts, or environment setup already exist; verify first.
- Prefer **TypeScript** on the frontend and explicit validation boundaries with **Zod** for API/UI contracts.
- Default to **local-first** AI integrations with Ollama unless a hosted provider is explicitly requested.
- Link to existing docs such as `README.md` instead of duplicating them here.
- Update this file as the repo grows with verified commands, structure, and examples.

