# Copilot Instructions

## Project

`my-own-chatbot-ai` is a personal chatbot web app. The repository now includes a **contract-first scaffold** with a checked-in `.NET 8` Minimal API backend in `backend/src/MyOwnChatbotAi.Api` and the canonical API contract in `contracts/chatbot-api.openapi.yml`.

The frontend is fully scaffolded under `frontend/` using **Vite + React + TypeScript** with Tailwind CSS, Zustand, and Zod. The backend Ollama client layer (`IOllamaClient`, `OllamaHttpClient`, `OllamaOptions`) is implemented and registered. Conversation endpoints still use an in-memory stub; the send-message flow is not yet wired to Ollama. Orleans orchestration is the planned next backend step. Treat the Orleans notes below as the planned direction for that next implementation step.

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
- Verified frontend commands:
  - `cd frontend && npm run build` — produces `frontend/dist/`
  - `cd frontend && npm run lint` — ESLint passes
  - `cd frontend && npm run dev` — starts Vite dev server
- There is currently **no dedicated test project** checked into the repo.
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
- **After completing any task**, stage all changed files, commit with a descriptive message, and push the branch to the remote:
  - Follow the imperative-mood commit style already used in this repo (e.g. `Add X`, `Fix Y`, `Update Z`).
  - Always include the co-author trailer in the commit message:
    ```
    Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
    ```
  - Push with `git push` (or `git push -u origin <branch>` for a new branch) so the remote stays up to date.

