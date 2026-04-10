---
description: "Use when setting up the local development environment, troubleshooting tool versions, or verifying prerequisites for the chatbot app."
name: "Dev Setup Guidelines"
applyTo: "**"
---
# Dev Setup Guidelines

## Prerequisites

Verify all tools are present before running any project commands.

- **.NET 8 SDK** — `dotnet --version` (must be 8.x)
- **Node.js 18+** — `node --version`
- **npm** — `npm --version`
- **Ollama** — `ollama --version` or check `http://localhost:11434`

Do not assume any of these are installed. Run the verification commands first.

## Backend Setup

```bash
# Build the solution
dotnet build backend/src/MyOwnChatbotAi.sln

# Run the API (available at http://localhost:5050)
dotnet run --project backend/src/MyOwnChatbotAi.Api
```

## Frontend Setup

```bash
# Install dependencies
cd frontend && npm install

# Start Vite dev server (available at http://localhost:3000)
cd frontend && npm run dev

# Build for production
cd frontend && npm run build

# Lint
cd frontend && npm run lint
```

> Before running any of the above, verify `frontend/package.json` exists.

## Ollama Setup

- Install from https://ollama.com
- Start the local model server:

```bash
ollama serve
# or pull and run a model directly
ollama run llama3.1
```

- The backend expects Ollama at `http://localhost:11434` by default.

## Docker Compose (Full Stack)

All Compose files live under `infrastructure/`. The build context is the repo root.

```bash
# Local dev (applies docker-compose.override.yml automatically)
cd infrastructure && docker compose up --build

# Production-like (no override)
cd infrastructure && docker compose -f docker-compose.yml up --build
```

## Node / npm on macOS

- Homebrew-managed Node may cause ICU-related errors with npm.
- Prefer **nvm** or the official Node.js installer from https://nodejs.org.
- Always confirm `node --version` and `npm --version` before suggesting or running npm commands.

## General Reminders

- **Do NOT assume** tools, folders, or scripts exist — verify with the commands above first.
- Only suggest commands that have been confirmed to work in this repo.
- Keep `README.md` and other docs in sync when verified commands change.
