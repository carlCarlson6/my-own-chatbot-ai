# Frontend Chat UI Implementation Plan

_Last updated: 2026-04-10_

## Purpose

This document is the execution handoff for wiring the frontend chat UI to the live backend API.

The repo already has a **Vite + React + TypeScript + Tailwind CSS + Zustand + Zod scaffold**. The next implementation agent should **replace the placeholder `App.tsx`** with a fully functional chat UI connected to the backend API endpoints defined in `contracts/chatbot-api.openapi.yml`.

---

## Current Verified Repo State

As of this update, the following already exist:

- `contracts/chatbot-api.openapi.yml` — public API contract source of truth
- `frontend/src/App.tsx` — placeholder component (`"Chatbot UI — coming soon"`)
- `frontend/src/main.tsx` — Vite entry point with `StrictMode` and Tailwind CSS import
- `frontend/src/index.css` — global styles
- `frontend/package.json` — all required packages installed:
  - `react` + `react-dom` (v19)
  - `zustand` (v5) — shared state
  - `zod` (v4) — runtime validation
  - `axios` (v1) — HTTP client
  - `tailwindcss` (v4) — utility-first CSS
  - `typescript` (v6) — type safety
- No `api/`, `components/`, `store/`, or `types/` directories exist yet — all must be created

Verified frontend commands from `README.md`:

```bash
cd frontend && npm run build   # tsc -b && vite build
cd frontend && npm run lint    # ESLint
```

---

## Goal

A working single-page chat UI that:

- Renders a clean, responsive chat interface with a message list and composer input
- Sends user messages to `POST /api/conversations/send` and displays assistant replies
- Maintains conversation state (messages, selected model, loading/error status) in a Zustand store
- Validates all API responses at the boundary with Zod before touching state
- Allows selecting between available Ollama models from `GET /api/models`
- Shows clear UI states for `idle`, `sending`, and `error` conditions
- Passes `npm run build` and `npm run lint` with zero errors

### MVP Constraints

- **No token streaming in v1** — send/receive as a complete round-trip
- **Single active conversation per session** — no conversation switcher
- **No authentication** — open access matching the backend contract
- **No persistence** — conversation state lives in memory; refresh resets to a new conversation

---

## Pre-Implementation Change Review

Before starting any implementation work, the next agent should first check what has changed since this plan was last updated and refresh if needed.

### Required pre-flight review

1. Review the latest project state before coding:
   - inspect `README.md`
   - inspect `contracts/chatbot-api.openapi.yml`
   - inspect `frontend/src/` directory
   - inspect `frontend/package.json`
2. Compare the current repo against this document and identify whether any of the following already changed:
   - API endpoint surface or DTO shapes
   - installed packages (`zod`, `zustand`, `axios` versions)
   - any partially-implemented component or store files
   - `vite.config.ts` proxy settings (needed for local dev API routing)
3. If changes make this plan stale, update this file first before implementing.
4. Only after that review should the agent begin the actual coding steps.

### Why this matters

Another agent may have already completed part of the scaffold or changed the frontend structure. This plan must stay aligned with the **current repo state**, not just the state when this document was first written.

---

## Non-Negotiable Architecture Rules

1. **Frontend never calls Ollama directly.**
   - Allowed: `Frontend → Backend API → Orleans → Ollama`
   - Disallowed: `Frontend → Ollama`

2. **All API responses must be Zod-validated before reaching the store.**
   - Parse every response shape at the boundary
   - Reject malformed responses with a typed error state — do not silently pass through raw data

3. **Zustand store owns shared conversation state.**
   - Transient component state (e.g. input field value) belongs in `useState`
   - Cross-component state (messages, status, selected model) belongs in the store

4. **No transport DTOs in component code.**
   - Map API response shapes to frontend domain types before they enter the store
   - Components depend on store types, not raw API response shapes

5. **Keep components small and composable.**
   - Separate presentational components from orchestration logic
   - Each component has a single clear responsibility

---

## Proposed File Structure

```
frontend/src/
├── api/
│   ├── client.ts              # Axios instance with base URL and interceptors
│   ├── conversations.ts       # Typed fetch functions for conversation endpoints
│   ├── models.ts              # Typed fetch function for /api/models
│   └── schemas.ts             # Zod schemas matching the OpenAPI contract
├── components/
│   ├── ChatLayout.tsx         # Root layout: sidebar (future) + main chat area
│   ├── ConversationHeader.tsx # Displays conversation title and model name
│   ├── MessageList.tsx        # Scrollable list of chat messages
│   ├── MessageBubble.tsx      # Single message bubble (user or assistant)
│   ├── MessageComposer.tsx    # Textarea + send button
│   └── ModelSelector.tsx      # Dropdown to pick available Ollama model
├── store/
│   └── chatStore.ts           # Zustand store — conversation, messages, status
├── types/
│   └── chat.ts                # Frontend domain types (ChatMessage, ConversationState, etc.)
├── App.tsx                    # Root component — wires store + layout
├── index.css                  # Global/Tailwind styles
└── main.tsx                   # Vite entry point (unchanged)
```

---

## Execution Plan

### Phase 1 — API client layer ⏳ Pending

**Goal:** typed, Zod-validated fetch functions that mirror every endpoint in `contracts/chatbot-api.openapi.yml`.

#### Tasks

1. Create `src/types/chat.ts` with frontend domain types:
   - `ChatMessage` — `{ messageId, role, content, createdAtUtc }`
   - `Conversation` — `{ conversationId, title, model, status, messages }`
   - `ModelSummary` — `{ name, displayName, isDefault, description? }`
   - `ApiError` — `{ code, message, target?, details? }`
2. Create `src/api/schemas.ts` with Zod schemas for all response shapes:
   - `chatMessageSchema` → `ChatMessage`
   - `createConversationResponseSchema` → `CreateConversationResponse`
   - `sendMessageResponseSchema` → `SendMessageResponse`
   - `getConversationHistoryResponseSchema` → `GetConversationHistoryResponse`
   - `listModelsResponseSchema` → `ListModelsResponse`
   - `apiErrorSchema` → `ApiError`
3. Create `src/api/client.ts`:
   - Axios instance with `baseURL` pointing to `http://localhost:5050` (or a Vite proxy path)
   - Default `Content-Type: application/json` header
   - Response interceptor that extracts typed `ApiError` from 4xx/5xx responses
4. Create `src/api/conversations.ts` with three typed functions:
   - `createConversation(req?: CreateConversationRequest): Promise<CreateConversationResponse>`
   - `sendMessage(req: SendMessageRequest): Promise<SendMessageResponse>`
   - `getConversationHistory(conversationId: string): Promise<GetConversationHistoryResponse>`
5. Create `src/api/models.ts`:
   - `listModels(): Promise<ListModelsResponse>`
6. Update `vite.config.ts` to add a dev server proxy:
   - `/api` → `http://localhost:5050` so the browser avoids CORS issues in development

#### Files to create/update

- `frontend/src/types/chat.ts` _(new)_
- `frontend/src/api/schemas.ts` _(new)_
- `frontend/src/api/client.ts` _(new)_
- `frontend/src/api/conversations.ts` _(new)_
- `frontend/src/api/models.ts` _(new)_
- `frontend/vite.config.ts` _(update — add proxy)_

#### Acceptance criteria

- All Zod schemas compile without errors
- Each fetch function returns a strongly-typed result or throws a typed `ApiError`
- `npm run build` passes with zero TypeScript errors

---

### Phase 2 — Zustand store ⏳ Pending

**Goal:** a single Zustand store that owns all conversation and UI state the components depend on.

#### Tasks

1. Create `src/store/chatStore.ts` with the following state shape:

   ```ts
   interface ChatState {
     // Conversation
     conversationId: string | null
     messages: ChatMessage[]
     model: string
     availableModels: ModelSummary[]

     // UI status
     status: 'idle' | 'sending' | 'error'
     errorMessage: string | null

     // Actions
     setModel: (model: string) => void
     loadModels: () => Promise<void>
     sendMessage: (content: string) => Promise<void>
     clearError: () => void
   }
   ```

2. Implement `loadModels()`:
   - Calls `listModels()` from the API layer
   - Populates `availableModels` and sets `model` to the default if none is selected yet
3. Implement `sendMessage(content)`:
   - Sets `status = 'sending'`
   - Calls `sendMessage({ conversationId, model, message: { content } })`
   - On success: appends both `userMessage` and `assistantMessage` to `messages`, updates `conversationId`, sets `status = 'idle'`
   - On error: sets `status = 'error'` and `errorMessage`
4. Use `immer` middleware only if needed; prefer plain state updates for the MVP

#### Files to create

- `frontend/src/store/chatStore.ts` _(new)_

#### Acceptance criteria

- Store compiles with zero TypeScript errors
- `sendMessage` optimistically appends the user message and awaits the assistant reply
- Error state is reachable and clearable
- `npm run build` passes

---

### Phase 3 — Core chat components ⏳ Pending

**Goal:** build the presentational layer — each component with a clear, single responsibility.

#### Tasks

1. **`MessageBubble.tsx`** — single message row:
   - Different alignment/colour for `user` vs `assistant` roles
   - Renders `content` as plain text (no markdown rendering in MVP)
   - Shows timestamp in a readable locale format
2. **`MessageList.tsx`** — scrollable message feed:
   - Renders a list of `MessageBubble` components
   - Auto-scrolls to the bottom when new messages arrive (`useEffect` + `scrollIntoView`)
   - Shows a centered placeholder when `messages` is empty
3. **`MessageComposer.tsx`** — input + send:
   - `<textarea>` for multi-line input with `Shift+Enter` for newlines, `Enter` to send
   - Send button disabled while `status === 'sending'`
   - Clears input on successful send
   - Shows inline spinner while `sending`
4. **`ModelSelector.tsx`** — model dropdown:
   - `<select>` populated from `availableModels` in the store
   - Disabled while `status === 'sending'`
   - Calls `setModel()` on change
5. **`ConversationHeader.tsx`** — top bar:
   - Displays current model via `ModelSelector`
   - Placeholder for conversation title (hardcoded `"New Conversation"` in MVP)
6. **`ChatLayout.tsx`** — root layout:
   - Full-viewport flex column: `ConversationHeader` → `MessageList` (flex-1, overflow-y-auto) → `MessageComposer`
   - Responsive, clean dark-mode default (Tailwind `bg-gray-950 text-gray-100`)

#### Files to create

- `frontend/src/components/MessageBubble.tsx` _(new)_
- `frontend/src/components/MessageList.tsx` _(new)_
- `frontend/src/components/MessageComposer.tsx` _(new)_
- `frontend/src/components/ModelSelector.tsx` _(new)_
- `frontend/src/components/ConversationHeader.tsx` _(new)_
- `frontend/src/components/ChatLayout.tsx` _(new)_

#### Acceptance criteria

- All components render without runtime errors
- `MessageList` auto-scrolls to the latest message
- `MessageComposer` clears after send and is disabled while sending
- `ModelSelector` reflects the store's current model selection
- `npm run build` and `npm run lint` both pass

---

### Phase 4 — End-to-end wiring ⏳ Pending

**Goal:** connect the store and components into a fully working send → API → update → re-render loop.

#### Tasks

1. Update `App.tsx`:
   - Replace the placeholder with `<ChatLayout />`
   - Call `loadModels()` on mount (`useEffect`) to populate the model list
2. Wire `MessageComposer` to `chatStore.sendMessage()`
3. Wire `MessageList` to `chatStore.messages`
4. Wire `ModelSelector` to `chatStore.availableModels` and `chatStore.setModel()`
5. Wire `ConversationHeader` to the active model name
6. Manually test the full flow with the backend running:
   ```bash
   dotnet run --project backend/src/MyOwnChatbotAi.Api
   cd frontend && npm run dev
   ```
   - Type a message → press Enter → verify user message appears → verify assistant reply appears

#### Files to update

- `frontend/src/App.tsx` _(update)_
- Component files from Phase 3 as needed for store wiring

#### Acceptance criteria

- Full round-trip works: type → send → user bubble → assistant bubble
- `conversationId` is created automatically on the first message send
- Model selector defaults to the backend's default model
- Subsequent messages in the same session use the same `conversationId`
- `npm run build` passes

---

### Phase 5 — Loading, error, and polish ⏳ Pending

**Goal:** production-ready UX states and clean visual polish.

#### Tasks

1. **Loading state** — while `status === 'sending'`:
   - Show a typing indicator / "thinking…" bubble in `MessageList`
   - Disable the send button and composer input
2. **Error state** — when `status === 'error'`:
   - Display an inline error banner below the message list with `errorMessage`
   - Provide a dismiss/retry action via `chatStore.clearError()`
3. **Empty state** — when `messages` is empty:
   - Show a centred welcome message or prompt suggestion
4. **Visual polish**:
   - Consistent bubble padding, border radius, and colour contrast
   - Timestamp rendering below each bubble
   - Send button with a clear icon or label
   - Accessible `aria-label` attributes on interactive elements
5. **`vite.config.ts` proxy** (if not done in Phase 1):
   - Ensure `/api` requests proxy to `http://localhost:5050` so `npm run dev` works without CORS

#### Files to update

- `frontend/src/components/MessageList.tsx`
- `frontend/src/components/MessageComposer.tsx`
- `frontend/src/components/ChatLayout.tsx`
- `frontend/vite.config.ts` (if proxy not yet added)

#### Acceptance criteria

- All three UI states (`idle`, `sending`, `error`) are visually distinct
- Error banner is dismissible
- Empty state renders correctly on first load
- `npm run build` and `npm run lint` both pass with zero errors

---

## Acceptance Criteria (Overall)

The implementation is ready when all of the following are true:

- Sending a message via the UI returns a real Ollama-backed assistant reply
- All API responses are Zod-validated before reaching the Zustand store
- The Zustand store owns conversation state, messages, model selection, and UI status
- `MessageList`, `MessageComposer`, `ConversationHeader`, and `ModelSelector` are distinct components
- `npm run build` succeeds with zero TypeScript errors
- `npm run lint` passes with zero ESLint errors
- The frontend never makes direct HTTP calls to Ollama (`localhost:11434`)
- `README.md` is updated to reflect any new verified commands or config

---

## Verification Commands

Do **not** mark the work complete until these checks pass:

```bash
# Type-check and build
cd frontend && npm run build

# Lint
cd frontend && npm run lint
```

Manual smoke test (requires backend + Ollama running):

```bash
# Start the backend
dotnet run --project backend/src/MyOwnChatbotAi.Api

# Start the frontend dev server
cd frontend && npm run dev
# Open http://localhost:5173, type a message, verify full round-trip
```

---

## Out of Scope for This Plan

These items are intentionally excluded from this implementation pass:

- Token streaming (deferred; backend does not yet stream responses)
- Conversation history browser / sidebar (single conversation only)
- Markdown/code rendering in assistant messages
- Authentication and authorization
- Database persistence (conversation resets on refresh)
- Mobile-specific layout optimizations beyond basic responsiveness
- Accessibility audit beyond basic `aria-label` attributes

---

## Suggested Execution Order for the Next Agent

1. Run the pre-implementation change review
2. Read `contracts/chatbot-api.openapi.yml` for the authoritative DTO shapes
3. Implement Phase 1 (types, Zod schemas, API client) and confirm `npm run build` passes
4. Implement Phase 2 (Zustand store) and confirm it compiles
5. Implement Phase 3 (components) and confirm `npm run build` + `npm run lint` pass
6. Implement Phase 4 (wire `App.tsx`) and manually verify the end-to-end flow
7. Implement Phase 5 (polish) and run final build + lint verification
8. Update `README.md` with any newly verified commands or setup steps
