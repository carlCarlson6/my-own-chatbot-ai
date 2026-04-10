# my-own-chatbot-ai

My own chatbot web app.

## Tech Stack

### Backend
- **.NET** with **Microsoft Orleans** (actor model framework)

### Frontend
- **Vite** + **React** + **TypeScript**
- **Tailwind CSS** for styling
- **Zustand** for state management
- **Zod** for schema validation

### AI
- **Ollama** for LLM inference

## Current Status

- Initial implementation has started with a **contract-first** scaffold.
- Canonical API contract: `contracts/chatbot-api.openapi.yml`
- Scaffolding plan: `docs/scaffolding-plan.md`
- The backend scaffold now exists under `backend/` and includes a working minimal chat API stub.
- The frontend scaffold is the next step; the local JavaScript toolchain currently needs a small npm/Node repair or workaround.

## Verified Backend Commands

- `dotnet build backend/src/MyOwnChatbotAi.sln`
- `dotnet run --project backend/src/MyOwnChatbotAi.Api`

## Next Steps

1. Add the Orleans-backed conversation flow behind the current in-memory service
2. Scaffold the `Vite + React + TypeScript` frontend under `frontend/`
3. Wire the first end-to-end send-message flow
