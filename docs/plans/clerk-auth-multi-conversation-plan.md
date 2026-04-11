# Clerk-Enabled Multi-Conversation Plan

_Last updated: 2026-04-11_

## Goal

Add multi-conversation chat to the app while preserving the current anonymous single-chat experience. Clerk must be introduced so signed-in users can access saved, user-owned multiple conversations via a left sidebar, switch between conversations with full history loading, rename conversations, delete conversations, and use default title generation from the first user message.

## Pre-Implementation Change Review

Before starting implementation work, first inspect the current repo state and confirm this plan still matches reality. If commands, structure, status, or acceptance criteria changed, refresh the plan before coding.

### Required review steps
1. Read `README.md`, `contracts/chatbot-api.openapi.yml`, and the affected implementation folders.
2. Compare the current repo against this plan: endpoints, DTOs, service registrations, status markers.
3. Update this plan first if any step is stale or already completed.
4. Only after that review should implementation continue.

## Current Repo Reality

| Area | Current state | Gap for this feature |
|---|---|---|
| Authentication | Backend now validates optional Clerk bearer tokens and exposes a current-user abstraction; anonymous conversation routes remain open | Frontend Clerk UX/runtime wiring and authenticated conversation ownership flows still need to be completed |
| API contract | Declares Clerk bearer auth for future protected routes and marks create/send/history as anonymous-capable | Protected conversation-management endpoints and user-scoped response shapes are still missing |
| Backend conversation model | Orleans `ConversationGrain` stores one conversation per grain with in-memory grain storage and no user ownership | User scoping, listing, rename/delete flows, and durable storage strategy are missing |
| Frontend chat UX | Single active conversation in `chatStore.ts`, no sidebar, no auth gate | Needs multi-conversation state, sidebar UI, and auth-aware API calls |
| Infrastructure | App stack exists for frontend/backend/Ollama only | Clerk env vars/secrets and any persistence dependency must be wired into infra/docs |

## Assumptions

- Unauthenticated users keep a single chat experience only. Clerk sign-in is required to unlock saved multi-conversation management.
- Conversations must be tied to a Clerk user and remain available after page refreshes. The current in-memory-only setup is not enough for this expectation, so persistence work is part of the plan.
- Deleting a conversation is a hard delete of that conversation and its stored message history for the owning user.
- The default title is derived from the **first user message only**: trim whitespace, keep the first 100 characters, and append `...` when the message exceeds 100 characters.
- If a conversation exists before the first user message is sent, it uses a temporary fallback title such as `New conversation` until the first user message arrives.

## Agent Workstreams

| Agent | Primary responsibilities | Depends on |
|---|---|---|
| `danny` | Break work into phases, sequence handoffs, keep contract/backend/frontend/infra aligned, and run integration review | Full repo review |
| `isabel` | Review and maintain the OpenAPI contract first for auth requirements and conversation-management endpoints/schemas | Goal and UX rules confirmed |
| `salva` | Implement backend auth integration, user-owned conversation model, endpoints, ownership checks, and title rules | Contract updates |
| `aitor` | Integrate Clerk in the frontend, add auth-aware UX and token-aware API client calls, preserve anonymous single-chat flow, and build the multi-conversation sidebar experience | Contract + backend auth/API support |
| `ivan` | Review AI/Ollama implications of per-conversation ownership and history loading; adjust AI-side configuration only if needed | Backend conversation flow decisions |
| `vicente` | Add Clerk env/config wiring, secret documentation, container/Kubernetes updates, and any persistence-related infrastructure changes | Backend/frontend runtime requirements |

## Recommended Execution Order

1. Update the contract and document the mixed access model: anonymous single chat plus authenticated multi-conversation management.
2. Add backend Clerk authentication and user identity propagation.
3. Add durable, user-owned conversation persistence and conversation-management endpoints.
4. Integrate Clerk in the frontend and ship the sidebar/multi-conversation UX.
5. Wire runtime configuration and secrets in infrastructure.
6. Run cross-agent integration review and verification.

## Phase 1 — Contract and Clerk Access Model Foundation ✅ Done

Establish Clerk as the capability that unlocks saved multi-conversation features while preserving anonymous single-chat behavior.

### Planned work

- `contract-updater` ✅ Done
  - Add an auth scheme to `contracts/chatbot-api.openapi.yml` for protected conversation-management endpoints.
  - Document which routes remain anonymous-capable (`create`, `send`, active history flow) and which require sign-in (`list`, `rename`, `delete`, saved multi-conversation retrieval).
  - Add `401` / `403` responses where authenticated access is required.
- `salva` ✅ Done
  - Integrate Clerk token validation into the backend request pipeline.
  - Introduce a backend abstraction for the authenticated user id/claims used by conversation flows.
  - Ensure protected conversation-management endpoints fail predictably when auth is missing or invalid, without breaking anonymous single-chat behavior.
- `aitor` ✅ Done
  - Add Clerk to the frontend shell (`ClerkProvider`, sign-in/sign-out, session awareness).
  - Preserve the anonymous chat entry point.
  - Expose a clear sign-in CTA to unlock multi-conversation history and management.
  - Attach Clerk auth tokens to backend API calls when a user is signed in.
- `vicente` ✅ Done
  - Document and wire the frontend publishable key and backend auth configuration into local/dev/deployment environments.

## Phase 2 — User-Owned Conversation Persistence ✅ Done

Add a user-scoped conversation store for authenticated users while leaving the anonymous single-chat path lightweight and non-persistent.

### Execution decision

Use a lightweight **SQLite-backed durable store** for authenticated conversation summaries and message history. This keeps the project local-first, avoids introducing a separate database service before it is needed, and gives later phases a concrete persistence target for backend and infrastructure work.

### Planned work

- `salva` ✅ Done
  - Define the canonical backend model for a user-owned conversation summary and message history.
  - Choose and implement a persistence approach that survives refreshes and supports efficient list/history lookups.
  - Add ownership checks so one user cannot access another user's conversations.
  - Keep the anonymous path limited to one non-managed conversation flow.
  - Update title-generation logic so the first user message becomes the default title when no manual title exists.
- `ivan` ✅ Done
  - Confirmed that persisted ordered history is rehydrated into `ConversationGrain` and then forwarded as the full `OllamaMessage[]` input, so reopening a saved conversation preserves the Ollama chat context expected by the current backend flow.
  - Review note: there is currently no truncation, summarization, or context-budget guardrail. If authenticated histories grow large, SQLite reload cost, request payload size, Ollama latency, and timeout/context-window pressure will all grow linearly and should be addressed in a later AI/runtime pass.
- `vicente` ✅ Done
  - Wired the backend SQLite database path to durable Docker Compose and Kubernetes storage, added backend PVC/volume mounts, and documented the runtime/storage expectations for the new persistence dependency.

## Phase 3 — Conversation Management API Surface ⏳ Pending

Add the backend contract and endpoints required to browse, rename, delete, and reopen saved conversations for authenticated users.

### Planned work

- `isabel`
  - Add `GET /api/conversations` returning user-scoped `ConversationSummary[]`.
  - Add `PATCH /api/conversations/{conversationId}` for renaming.
  - Add `DELETE /api/conversations/{conversationId}` for deletion.
  - Update existing create/send/history schemas if they must carry additional summary metadata or clarify anonymous-vs-authenticated behavior.
- `salva`
  - Implement the new endpoints in feature slices under `backend/src/MyOwnChatbotAi.Api/Features/Conversations/`.
  - Keep saved-conversation history owner-aware and auth-protected while preserving the anonymous single-chat history path if it remains part of the existing flow.
  - Return consistent `404`, `401`, and `403` behavior for unknown, unauthenticated, and unauthorized access on protected routes.

## Phase 4 — Frontend Sidebar and Multi-Conversation UX ⏳ Pending

Add the left-hand conversation panel for signed-in users while keeping the anonymous user experience focused on one active chat.

### Planned work

- `aitor`
  - Extend the frontend API client and Zod schemas for list/rename/delete operations.
  - Refactor the Zustand store to hold:
    - `conversations`
    - `activeConversationId`
    - active conversation messages/history
    - loading/error state for sidebar operations
  - Add a left sidebar showing the signed-in user's conversations.
  - Clicking a conversation loads its history and activates it.
  - Add a "New conversation" action that clears the active panel and starts a fresh conversation on the next send.
  - Add edit and delete buttons on each conversation row.
  - Keep anonymous users on the single-chat layout, with a prompt to sign in for multi-conversation access.
  - Preserve the existing chat layout behavior on small screens with a responsive/collapsible sidebar treatment.
- `danny`
  - Ensure frontend sequencing waits for backend contract/auth readiness before UI wiring begins.

## Phase 5 — AI/Runtime and Infrastructure Alignment ⏳ Pending

Keep the AI runtime and deployment setup aligned with the authenticated multi-conversation design.

### Planned work

- `ivan`
  - Review whether per-user conversation loading changes any prompt-assembly, model-selection, or context-window assumptions.
  - Recommend AI-focused guardrails only if history growth introduces quality or performance risk.
- `vicente`
  - Add Clerk-related variables to Compose/Kubernetes docs and manifests as needed.
  - Keep secrets out of the repo and document variable names only.
  - Update infra docs if new services, volumes, or env vars are introduced.

## Phase 6 — Integration Review and Verification ⏳ Pending

Confirm the full feature behaves coherently across the anonymous single-chat path, authenticated ownership rules, frontend UX, and deployment/runtime configuration.

### Planned work

- `danny`
  - Verify dependency order was respected: contract -> backend auth/persistence -> frontend -> infra.
  - Confirm the final flow for anonymous single chat, sign-in, conversation creation, switching, rename, delete, and reload.
  - Route final project-aware review to `juanjo` for a correctness pass on the integrated changes.

## Acceptance Criteria

- Unauthenticated users can still use a single chat without signing in.
- Sign-in is required to access saved multi-conversation features, including the sidebar list, rename, delete, and reopening prior conversations.
- Backend protected conversation-management endpoints are user-scoped and reject unauthenticated or unauthorized access correctly.
- A signed-in user can create multiple conversations and see them in a left sidebar.
- Clicking a conversation loads its full saved message history into the active chat view.
- Each sidebar conversation item supports rename and delete actions.
- Anonymous users do not see or access the multi-conversation sidebar and remain limited to one active chat.
- The default conversation title is derived from the first user message, limited to 100 characters, with `...` appended when truncated.
- Manual renaming overrides the generated title without mutating historical messages.
- Deleting a conversation removes it from the sidebar and prevents further history retrieval for that user.
- `README.md` and any infra docs reflect the required Clerk configuration and any new persistence/runtime prerequisites.
