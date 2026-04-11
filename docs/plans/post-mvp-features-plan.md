# Post-MVP Features Plan

_Last updated: 2026-04-11_

## Goal

Realign the post-MVP roadmap to the current repo reality and keep it ready for Danny-led orchestration. The remaining post-MVP work is now:

1. token streaming for real-time assistant responses
2. conversation history budget / truncation guardrails for long saved chats
3. test infrastructure for backend and frontend maintainability

The old sidebar work is no longer part of this plan because the signed-in multi-conversation experience already shipped under [`docs/plans/old/clerk-auth-multi-conversation-plan.md`](old/clerk-auth-multi-conversation-plan.md).

## Pre-Implementation Change Review

Before starting implementation work, first inspect the current repo state and confirm this plan still matches reality. If commands, structure, status, or acceptance criteria changed, refresh the plan before coding.

### Required review steps
1. Read `README.md`, `contracts/chatbot-api.openapi.yml`, and the affected implementation folders.
2. Compare the current repo against this plan: endpoints, DTOs, service registrations, status markers, and test/tooling setup.
3. Update this plan first if any step is stale or already completed.
4. Only after that review should implementation continue.

## Current Repo Reality (Verified 2026-04-11)

| Area | Current state | Remaining gap for this plan |
|---|---|---|
| Contract | `contracts/chatbot-api.openapi.yml` already covers create/send/history plus authenticated list/rename/delete flows | No streaming contract yet; no history-budget metadata yet |
| Backend conversation API | `Create`, `Send`, `GetHistory`, `List`, `Rename`, and `Delete` conversation endpoints are wired under `Features/Conversations/` | No streaming endpoint; no long-history budget guardrail |
| Orleans + persistence | `ConversationGrain` owns active state and rehydrates authenticated histories from SQLite-backed persistence | Every send still replays the full stored history to Ollama; no truncation/summarization budget |
| Ollama client | `IOllamaClient` exposes `ListModelNamesAsync` and non-streaming `ChatAsync` only | No chunked/streaming chat abstraction |
| Frontend chat UX | Clerk-aware sidebar, history switching, rename/delete, and auth-aware send flow already exist | Store/UI still use request/response send only; no partial-token rendering |
| Test infrastructure | No backend test project; no frontend test dependencies, scripts, or test files | Full backend/frontend test harness still needs to be added |
| Plan drift | This plan still referenced a pending sidebar phase and old `/api/models` test coverage assumptions | Refresh phase order, ownership, and acceptance criteria to match the current repo |

## Scope Adjustments from the Previous Revision

- The conversation sidebar and authenticated multi-conversation flow are treated as **completed** and remain tracked in the Clerk plan, not here.
- A new post-MVP phase for **conversation history budget guardrails** is introduced because the repo now persists and reloads longer authenticated histories.
- The old `/api/models` testing task is removed from this plan because the currently inspected backend source tree exposes conversation slices only; this plan's test scope now follows the implemented conversation/auth surfaces.

## Orchestration Entry Points

Use this plan with the repo's orchestration prompts:

1. `execute-plan-orchestrated.prompt.md` for a Danny-led single-entry execution flow across specialists
2. `start-plan-task.prompt.md` when a worker agent is manually asked to execute only its next owned task block

The plan is structured so the **first pending phase** and the **first pending task block inside that phase** are always unambiguous.

## Agent Workstreams

| Agent | Responsibilities in this plan | Depends on |
|---|---|---|
| `danny` | Keep the plan current, enforce phase order, manage handoffs, and run integration review | Full repo review |
| `isabel` | Contract-first updates for any new streaming or budget-related API/documented payload surface | Confirmed UX/runtime rules |
| `salva` | Backend streaming slice, Orleans/persistence updates, prompt-budget guardrails, backend tests | Contract updates or approved backend-only execution decisions |
| `aitor` | Frontend streaming UX, store/state updates, budget-related UI feedback if surfaced, frontend tests | Contract + backend support |
| `ivan` | Ollama/runtime review for chunk streaming, cancellation, and history-budget strategy | Backend conversation flow and streaming design |
| `vicente` | Infra/docs/runtime changes only if new env vars, timeout knobs, or test pipeline wiring become necessary | Backend/frontend runtime requirements |
| `juanjo` | Project-aware correctness review after meaningful cross-domain milestones or final completion | Implemented changes ready for review |

## Recommended Execution Order

1. Update the contract for streaming if the transport surface changes.
2. Ship backend streaming support and persistence-safe completion behavior.
3. Ship frontend streaming UX on top of the merged backend/API shape.
4. Define and implement long-history budget guardrails without regressing saved-history retrieval.
5. Add backend and frontend tests after the streaming/budget behavior settles.
6. Run cross-agent integration review and final verification.

## Phase 1 — Token Streaming ⏳ Pending

Add real-time assistant token streaming while preserving the existing non-streaming `POST /api/conversations/send` flow as a fallback.

### Repo reality notes

- The contract currently has no `/api/conversations/stream` route.
- `IOllamaClient` and `OllamaHttpClient` only expose non-streaming chat.
- `ConversationGrain.SendMessageAsync(...)` persists only after a complete assistant response is available.
- The frontend store only tracks `idle` / `sending` / `error` and does not render partial assistant output.

### Planned work

- `isabel` ✅ Done
  - Add `POST /api/conversations/stream` to `contracts/chatbot-api.openapi.yml`.
  - Reuse the existing `SendMessageRequest` shape unless repo reality shows a required streaming-specific field.
  - Document the `text/event-stream` response and define the event payloads needed for typed frontend/backend alignment (for example started/chunk/completed/error events).
  - Keep the existing non-streaming send route documented as a supported fallback path.
- `salva` ⏳ Pending
  - Extend `IOllamaClient` with a streaming abstraction appropriate for Ollama's chunked `/api/chat` responses.
  - Implement the streaming client path in `OllamaHttpClient` using response-header streaming and newline-delimited chunk parsing.
  - Add the backend streaming endpoint under `Features/Conversations/` and wire it through `MapConversationEndpoints()`.
  - Coordinate streaming completion with `ConversationGrain` so the user message and final assembled assistant message are persisted only when the stream completes successfully.
  - Ensure aborted or failed streams do not leave behind partial assistant messages or orphaned managed conversations.
- `aitor` ⏳ Pending
  - Add a typed frontend streaming API helper for `POST /api/conversations/stream`.
  - Extend the Zustand store with a `streaming` send state, optimistic assistant placeholder handling, and final-message reconciliation when the completion event arrives.
  - Update `MessageComposer`, `MessageList`, and `MessageBubble` so partial assistant output is visible while streaming.
  - Keep the current non-streaming send path available as a fallback until the streamed flow is stable.
- `ivan` ⏳ Pending
  - Review the Ollama/runtime implications of chunk streaming, including cancellation, timeout behavior, and final content assembly expectations.
  - Confirm whether any model/runtime guidance or follow-up constraints should be captured in this plan after backend/frontend implementation lands.
- `danny` ⏳ Pending
  - Hold downstream phase work until contract, backend, and frontend streaming work are aligned.
  - Run an integration pass for the streamed flow before marking this phase done.

## Phase 2 — Conversation History Budget Guardrails ⏳ Pending

Prevent very long saved conversations from replaying an unbounded transcript into Ollama on every send while still preserving the full stored history for retrieval and management.

### Repo reality notes

- Authenticated conversation history is persisted in SQLite and rehydrated into `ConversationGrain`.
- The current grain-to-Ollama flow replays the entire ordered history as `OllamaMessage[]` on every send.
- The Clerk plan already documented this as a follow-up risk for latency, timeout pressure, and context-window overflow.

### Preferred execution direction

Start with a **backend-owned history budget** that limits what is forwarded to Ollama while keeping the persisted conversation record intact. Only introduce contract/frontend changes if the chosen guardrail needs user-visible metadata or warnings.

### Planned work

- `ivan` ⏳ Pending
  - Define the initial history-budget strategy for the current architecture: what portion of the transcript must always be preserved, what budget signal to use first, and how overflow should be handled.
  - Recommend whether the first version should remain backend-only or whether the UI should surface truncation/budget status.
  - Record any AI/runtime tradeoffs that must constrain the backend implementation.
- `salva` ⏳ Pending
  - Implement the approved history-budget guardrail in the backend/Ollama preparation path without deleting persisted history from SQLite.
  - Keep anonymous and authenticated conversation behavior predictable when the budget applies.
  - Add any required backend-side metadata/logging that makes the new guardrail observable and debuggable without silent behavior changes.
- `isabel` ⏳ Pending
  - Update the contract only if the guardrail introduces user-visible response metadata, warning surfaces, or any new API shape.
- `aitor` ⏳ Pending
  - If the contract/backend expose budget metadata, surface it in the chat UI without disrupting the current send/sidebar flows.
  - Otherwise, limit frontend work in this phase to any typing/schema/test updates required by the final backend decision.
- `danny` ⏳ Pending
  - Confirm the chosen guardrail is documented in the plan and does not conflict with the already-shipped saved-conversation UX.

## Phase 3 — Test Infrastructure ⏳ Pending

Add real test coverage once the streaming and history-budget behavior is stable enough to lock in.

### Repo reality notes

- No `.csproj` test project exists under `backend/`.
- `frontend/package.json` has no `test` script and no Vitest/Testing Library/MSW dependencies.
- This repo's real validation surface today is build/lint only: backend build, frontend build, frontend lint.

### Planned work

- `salva` ⏳ Pending
  - Add a backend xUnit test project under `backend/tests/` and include it in `backend/src/MyOwnChatbotAi.sln`.
  - Introduce an integration-style test fixture around the Minimal API app using a deterministic fake `IOllamaClient`.
  - Cover the implemented conversation routes, including create/send/history plus authenticated list/rename/delete and streaming if Phase 1 is complete.
  - Keep test coverage aligned with the published contract and current auth model.
- `aitor` ⏳ Pending
  - Add frontend test tooling (`vitest`, React Testing Library, `@testing-library/user-event`, `msw`) and the corresponding `npm run test` script.
  - Add self-contained tests for the Zustand store and core chat components, including send/stream flows, sidebar behavior, and error states.
  - Keep API mocks aligned with the contract-backed Zod schemas.
- `danny` ⏳ Pending
  - Ensure the backend and frontend test scope matches the actual feature surface after Phases 1 and 2.
  - Update `README.md` with the real verified test commands once they exist and pass.
- `juanjo` ⏳ Pending
  - Review the combined test harness and feature coverage for correctness once the implementation work is staged.

## Phase 4 — Integration Review and Orchestrated Completion ⏳ Pending

Close the loop across contract, backend, frontend, runtime behavior, docs, and plan status so this plan can be executed cleanly end-to-end.

### Planned work

- `juanjo` ⏳ Pending
  - Run a project-aware review across the combined post-MVP changes and flag only material correctness issues.
- `danny` ⏳ Pending
  - Confirm the executed order stayed coherent: streaming contract -> backend -> frontend -> history budget -> tests -> review.
  - Update this plan's status markers, notes, and `_Last updated:` date after each meaningful milestone.
  - Move the README plan entry when every phase is marked `✅ Done`.

## Acceptance Criteria

### Phase 1 — Streaming
- [ ] `POST /api/conversations/stream` is documented in the contract and implemented in the backend.
- [ ] The frontend renders assistant output incrementally instead of waiting for the full reply.
- [ ] The final streamed assistant message is persisted and later returned by `GET /api/conversations/{conversationId}/history`.
- [ ] The existing non-streaming send flow remains available as a fallback during rollout.

### Phase 2 — History Budget
- [ ] Very long saved conversations no longer replay an unbounded transcript to Ollama on every send.
- [ ] Full stored history remains retrievable for conversation management and history loading.
- [ ] The chosen budget/truncation behavior is documented in this plan and implemented consistently with the current auth/persistence model.
- [ ] Any user-visible budget metadata is reflected consistently across contract, backend, and frontend.

### Phase 3 — Tests
- [ ] A backend test project exists and runs through the repo's real .NET test command.
- [ ] A frontend `npm run test` command exists and runs the committed component/store tests.
- [ ] Backend and frontend tests cover the actual conversation/auth/streaming flows implemented in the repo.
- [ ] `README.md` is updated with the verified test commands once they exist.

### Phase 4 — Integration
- [ ] The plan's phase order and ownership blocks remain accurate after each implementation milestone.
- [ ] A project-aware final review reports no unresolved material correctness issues.
- [ ] The README plan listing is updated when this plan becomes fully complete.
