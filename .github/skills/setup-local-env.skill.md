---
description: "Verify all local development prerequisites are installed and correctly configured for my-own-chatbot-ai. Checks .NET 8, Node.js 18+, npm, and Ollama, then guides through any missing setup."
name: "setup-local-env"
---

# setup-local-env

Verify that the local development environment for `my-own-chatbot-ai` is correctly set up. Check each prerequisite, report what's missing or misconfigured, and guide through the fix.

## Step 1 — Check prerequisites

Run each command and report the result:

| Tool | Command | Required version |
|---|---|---|
| .NET SDK | `dotnet --version` | 8.x |
| Node.js | `node --version` | 18+ |
| npm | `npm --version` | any recent |
| Ollama | `ollama --version` or `curl -s http://localhost:11434` | running |

If any check fails or returns the wrong version, report it clearly:
> ❌ `.NET SDK`: found 7.x, need 8.x — install from https://dotnet.microsoft.com/download/dotnet/8.0

## Step 2 — Verify project files exist

Confirm the real project files are present before suggesting any commands:

- `backend/src/MyOwnChatbotAi.sln` — .NET solution
- `backend/src/MyOwnChatbotAi.Api/MyOwnChatbotAi.Api.csproj` — API project
- `frontend/package.json` — frontend npm project
- `frontend/vite.config.ts` — Vite config
- `contracts/chatbot-api.openapi.yml` — API contract

## Step 3 — Verify backend builds

```bash
dotnet build backend/src/MyOwnChatbotAi.sln
```

Report: ✅ build succeeded / ❌ build failed (include error summary).

## Step 4 — Verify frontend installs and builds

```bash
cd frontend && npm install && npm run build
```

Report: ✅ build succeeded / ❌ build failed (include error summary).

## Step 5 — Verify Ollama is reachable

```bash
curl -s http://localhost:11434
```

If Ollama is not running:
```bash
ollama serve
```

## macOS note

If `npm` fails with ICU-related errors, Homebrew-managed Node is likely the cause.  
Prefer **nvm** or the official installer from https://nodejs.org.

## Output

Provide a summary table:

| Check | Status | Notes |
|---|---|---|
| .NET 8 SDK | ✅ / ❌ | version found |
| Node.js 18+ | ✅ / ❌ | version found |
| npm | ✅ / ❌ | version found |
| Ollama | ✅ / ❌ | running / not running |
| Backend build | ✅ / ❌ | — |
| Frontend build | ✅ / ❌ | — |

If all pass: "Environment is ready."  
If any fail: list remediation steps in priority order.
