# Copilot Instructions

## Project

`my-own-chatbot-ai` is a personal chatbot web app. The repository is currently an **early scaffold** with documentation only, so treat the structure below as the planned direction until real projects and source files are added.

See `README.md` for the current stack summary.

## Planned Architecture

- **Backend**: .NET with **Microsoft Orleans** for actor-style chat orchestration
- **Frontend**: **Vite + React + TypeScript** with Tailwind CSS, Zustand, and Zod
- **AI runtime**: **Ollama** for local LLM inference

Keep frontend and backend concerns clearly separated, and document integration boundaries when adding new pieces.

## Build and Test

- There are currently **no verified build, run, or test commands** in this repo.
- Before suggesting commands, first check for real project files such as `package.json`, `.sln`, `.csproj`, or `vite.config.*`.
- When scaffolding the app, add the verified commands to `README.md` and keep this file in sync.

## Conventions

- Keep changes minimal and aligned with the documented stack.
- Do **not** assume folders, scripts, or environment setup already exist; verify first.
- Prefer **TypeScript** on the frontend and explicit validation boundaries with **Zod** for API/UI contracts.
- Default to **local-first** AI integrations with Ollama unless a hosted provider is explicitly requested.
- Link to existing docs such as `README.md` instead of duplicating them here.
- Update this file as the repo grows with verified commands, structure, and examples.

