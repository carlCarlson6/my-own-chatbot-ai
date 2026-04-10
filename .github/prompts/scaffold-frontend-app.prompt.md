---
description: "Use when setting up or extending the frontend chat app with Vite, React, TypeScript, Tailwind CSS, Zustand, and Zod."
name: "Scaffold Frontend App"
---

# Scaffold Frontend App

You are working on the `my-own-chatbot-ai` frontend: a personal chatbot web app built with **Vite + React + TypeScript + Tailwind CSS + Zustand + Zod**, located under `frontend/`.

---

## Step 1 — Inspect current state first

Before writing any code, read the current repo state:

1. List the `frontend/` directory structure to understand what already exists.
2. Read `frontend/package.json` — check current dependencies, devDependencies, and scripts.
3. Read `frontend/src/` layout — understand existing components, hooks, and store structure.
4. Read `contracts/chatbot-api.openapi.yml` — this is the source of truth for all API shapes.

Do **not** assume any folder, file, or script exists. Verify first.

---

## Step 2 — Respect existing conventions

Follow these rules without deviation:

- **Build tool**: Vite + React + TypeScript only. Do not introduce alternative bundlers or frameworks.
- **Styling**: Tailwind CSS utility classes. Avoid large custom CSS files or CSS-in-JS solutions.
- **Global state**: Zustand for shared app state (chat session, streaming status, UI preferences). Use `useState` for transient component-local state only.
- **Validation**: Zod schemas at the API boundary. Always parse server responses before storing or rendering them.
- **Components**: Small, composable function components with explicit TypeScript types. Prefer typed custom hooks for logic reuse.

---

## Step 3 — Naming and structure conventions

Organise code by feature or domain, not by technical layer:

```
frontend/src/
  components/
    chat/
      MessageList.tsx       # Presentational: renders ordered messages
      MessageComposer.tsx   # Presentational: text input and send controls
      StatusIndicator.tsx   # Presentational: idle / sending / streaming / error
    models/
      ModelSelector.tsx     # Presentational: model selection control
  hooks/
    useChatSession.ts       # Orchestration: ties store, API calls, and streaming together
  store/
    chatStore.ts            # Zustand store: conversation state, streaming flag, selected model
  api/
    chatApi.ts              # API client: fetch calls to the backend
    schemas.ts              # Zod schemas mirroring OpenAPI contract shapes
  types/
    chat.ts                 # Frontend domain types: ChatMessage, ChatRequest, ChatResponse
```

Key separation rules:

- Presentational components own only rendering and local UI events.
- Orchestration logic (API calls, store updates, streaming state) lives in hooks, not components.
- `MessageList`, `MessageComposer`, `ModelSelector`, and `StatusIndicator` are always separate components.

---

## Step 4 — API integration

- The frontend calls **only** the backend API at `http://localhost:5050`. Never call Ollama directly from the frontend.
- Define Zod schemas in `frontend/src/api/schemas.ts` that closely mirror the OpenAPI shapes from `contracts/chatbot-api.openapi.yml`.
- Parse every API response through the matching Zod schema before passing data to the store or a component.
- Keep transport DTOs (shapes returned by the API) separate from UI view models when the two differ.
- Design for streaming assistant responses: handle `idle`, `sending`, `streaming`, and `error` states explicitly.

Example schema alignment pattern:

```ts
// contracts/chatbot-api.openapi.yml defines SendMessageResponse
// frontend/src/api/schemas.ts mirrors it:
import { z } from "zod";

export const SendMessageResponseSchema = z.object({
  conversationId: z.string().uuid(),
  message: z.object({
    id: z.string().uuid(),
    role: z.enum(["assistant"]),
    content: z.string(),
    createdAt: z.string().datetime(),
  }),
});

export type SendMessageResponse = z.infer<typeof SendMessageResponseSchema>;
```

---

## Step 5 — Validation before finishing

After all changes are made, run these commands and confirm both succeed:

```bash
cd frontend && npm run build
cd frontend && npm run lint
```

- Do **not** claim success without running these commands.
- If either command fails, fix the errors and re-run before finishing.
- Report the exact output of both commands in your summary.
