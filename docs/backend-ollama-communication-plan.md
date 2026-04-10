# Secure Backend ↔ Ollama Communication Plan

_Last updated: 2026-04-10_

## Purpose

This document is the execution handoff for wiring the real backend-to-Ollama flow.

The repo already has a **contract-first backend scaffold**. The next implementation agent should **replace the current in-memory stub** with a secure, backend-only Ollama integration coordinated by Orleans.

---

## Current Verified Repo State

As of this update, the following already exist:

- `contracts/chatbot-api.openapi.yml` — public API contract source of truth
- `backend/src/MyOwnChatbotAi.Api/` — Minimal API scaffold
- `backend/src/MyOwnChatbotAi.Api/Features/Conversations/` — thin conversation endpoints (still using in-memory stub)
- `backend/src/MyOwnChatbotAi.Api/Features/Models/ListModelsEndpoint.cs` — model listing endpoint (calls Ollama via `IOllamaClient`, falls back to allowlist)
- `backend/src/MyOwnChatbotAi.Api/Services/InMemoryConversationService.cs` — stub service for conversation flows
- `backend/src/MyOwnChatbotAi.Api/Services/Ollama/IOllamaClient.cs` — Ollama client abstraction ✅
- `backend/src/MyOwnChatbotAi.Api/Services/Ollama/OllamaHttpClient.cs` — HTTP implementation ✅
- `backend/src/MyOwnChatbotAi.Api/Services/Ollama/OllamaOptions.cs` — configuration type ✅
- `backend/src/MyOwnChatbotAi.Api/Services/Ollama/OllamaException.cs` — dedicated exception type ✅
- `backend/src/MyOwnChatbotAi.Api/appsettings.json` — Ollama config block (BaseUrl, DefaultModel, AllowedModels, TimeoutSeconds) ✅
- `docs/scaffolding-plan.md` — earlier scaffold plan

Current verified backend commands from `README.md`:

```bash
dotnet build backend/src/MyOwnChatbotAi.sln
dotnet run --project backend/src/MyOwnChatbotAi.Api
```

---

## Non-Negotiable Architecture Rules

1. **The frontend must never call Ollama directly.**
   - Allowed path: `Frontend -> Backend API -> Orleans -> Ollama client -> Ollama`
   - Disallowed path: `Frontend -> Ollama`

2. **Ollama must stay private/local-only by default.**
   - Use: `http://127.0.0.1:11434/api`
   - Do **not** expose `11434` publicly
   - Do **not** set `OLLAMA_HOST=0.0.0.0:11434` unless a later deployment model explicitly requires it

3. **Public API contracts must stay provider-agnostic.**
   - Keep `contracts/chatbot-api.openapi.yml` focused on app-level DTOs
   - Do not leak raw Ollama request/response payloads into the public contract

4. **Orleans should own conversation concurrency/state.**
   - The conversation grain should be the orchestration boundary
   - Avoid concurrency-sensitive chat state living in controller/endpoint code

5. **Security relies on backend-only routing + network isolation.**
   - Ollama local API has no required local auth
   - If stricter enforcement is needed later, add a local proxy with backend-only auth or mTLS

---

## Implementation Goal

Replace the stubbed `InMemoryConversationService` path with:

- `IOllamaClient` / `OllamaHttpClient`
- Orleans-backed conversation orchestration
- real Ollama-backed model listing and message generation
- loopback-only/private communication rules
- backend-side limits, timeout handling, and clean error mapping

---

## Pre-Implementation Change Review

Before starting any implementation work, the next agent should first check what changed in the repo since this plan was last updated and refresh the plan if needed.

### Required pre-flight review
1. Review the latest project state before coding:
   - inspect `README.md`
   - inspect `contracts/chatbot-api.openapi.yml`
   - inspect `docs/`
   - inspect `backend/src/MyOwnChatbotAi.Api/`
2. Compare the current repo against this document and identify whether any of the following already changed:
   - endpoint surface
   - DTOs/contracts
   - backend service registration in `Program.cs`
   - Orleans setup
   - any existing Ollama client implementation
   - security-related config in `appsettings*.json`
3. If changes make this plan stale, update this file first before implementing.
4. Only after that review should the agent begin the actual coding steps below.

### Why this matters
Another agent may have already completed part of the scaffold or changed the backend structure. This plan must stay aligned with the **current repo state**, not just the state when the document was first written.

---

## Execution Plan

### Phase 1 — Preserve the public API boundary ✅ Done

**Goal:** keep the existing public contract stable while swapping the internals.

#### Tasks
- Review `contracts/chatbot-api.openapi.yml` before any DTO/endpoint change
- Keep `ChatContracts.cs` aligned with the contract
- Preserve the existing endpoint surface unless the contract is explicitly updated first

#### Files
- `contracts/chatbot-api.openapi.yml`
- `backend/src/MyOwnChatbotAi.Api/Contracts/ChatContracts.cs`

---

### Phase 2 — Add the backend-only Ollama integration layer ✅ Done

**Goal:** centralize all communication with Ollama in one dedicated client.

#### Tasks
1. Add configuration for Ollama in backend settings, for example:
   - base URL: `http://127.0.0.1:11434/api`
   - default model
   - model allowlist
   - timeout seconds
2. Add an `OllamaOptions` configuration type
3. Add an `IOllamaClient` abstraction
4. Implement `OllamaHttpClient` using `HttpClient`
5. Centralize:
   - request serialization
   - cancellation tokens
   - timeout handling
   - error mapping
   - retry boundaries where appropriate
6. Update model listing to use real Ollama data via `/api/tags` or a config-backed allowlist

#### Recommended files to add/update
- `backend/src/MyOwnChatbotAi.Api/appsettings.json`
- `backend/src/MyOwnChatbotAi.Api/appsettings.Development.json`
- `backend/src/MyOwnChatbotAi.Api/Program.cs`
- `backend/src/MyOwnChatbotAi.Api/Services/Ollama/IOllamaClient.cs`
- `backend/src/MyOwnChatbotAi.Api/Services/Ollama/OllamaHttpClient.cs`
- `backend/src/MyOwnChatbotAi.Api/Features/Models/ListModelsEndpoint.cs`

---

### Phase 3 — Move chat orchestration into Orleans ✅ Done

**Goal:** make Orleans the real conversation coordination boundary.

#### Tasks
1. Add Orleans hosting/services in `Program.cs`
2. Create `IConversationGrain` and `ConversationGrain`
3. Let the grain own:
   - conversation id
   - title
   - selected model
   - ordered messages
   - timestamps
   - processing status
4. Have the grain call `IOllamaClient`
5. Refactor the endpoints so they delegate to Orleans-backed flows instead of the in-memory stub

#### Recommended files to add/update
- `backend/src/MyOwnChatbotAi.Api/Program.cs`
- `backend/src/MyOwnChatbotAi.Api/Grains/IConversationGrain.cs`
- `backend/src/MyOwnChatbotAi.Api/Grains/ConversationGrain.cs`
- `backend/src/MyOwnChatbotAi.Api/Features/Conversations/CreateConversationEndpoint.cs`
- `backend/src/MyOwnChatbotAi.Api/Features/Conversations/SendMessageEndpoint.cs`
- `backend/src/MyOwnChatbotAi.Api/Features/Conversations/GetConversationHistoryEndpoint.cs`

---

### Phase 4 — Security hardening ✅ Done

**Goal:** ensure only the backend is intended to communicate with Ollama.

**Already in place:**
- Ollama bound to loopback only: `127.0.0.1:11434` (via `OllamaOptions.BaseUrl` default)
- Model allowlist enforced in `OllamaOptions.AllowedModels`
- Request timeout configured in `OllamaOptions.TimeoutSeconds`
- `IOllamaClient` isolates Ollama URL from endpoint code

**Still required:**
- Max input/message size validation in conversation endpoints
- Clean `ApiError` responses for all Ollama-related failures (not only model listing)
- Redact or minimize sensitive prompt logging

#### Stronger hardening option (future)
If loopback-only is not strong enough for your setup, add one of these:

- a local reverse proxy in front of Ollama with a backend-only shared secret
- mTLS between backend and proxy
- a private Docker/network boundary where Ollama is not reachable from outside the backend container network

---

### Phase 5 — Verification checklist

Do **not** mark the work complete until these checks are run successfully.

#### Build/run checks
```bash
dotnet build backend/src/MyOwnChatbotAi.sln
dotnet run --project backend/src/MyOwnChatbotAi.Api
```

#### API smoke checks
```bash
curl http://localhost:5050/api/models
curl -X POST http://localhost:5050/api/conversations \
  -H "Content-Type: application/json" \
  -d '{"title":"Test","model":"llama3.1"}'
```

#### Ollama reachability/security checks
```bash
curl http://127.0.0.1:11434/api/tags
lsof -iTCP:11434 -sTCP:LISTEN -nP
```

Expected result:
- Ollama responds locally
- listener is bound to `127.0.0.1:11434` rather than `0.0.0.0` or `*`
- frontend/browser still uses only backend routes such as `http://localhost:5050/api/...`

---

## Acceptance Criteria

The implementation is ready when all of the following are true:

- `POST /api/conversations/send` returns a **real Ollama-backed assistant reply**
- conversation state is orchestrated through **Orleans**, not only the in-memory stub
- `GET /api/models` returns real/approved model information
- the public OpenAPI contract remains app-level and does not expose Ollama internals
- Ollama remains private to the backend path
- `README.md` is updated with any newly verified configuration and run steps

---

## Out of Scope for This Plan

These items are intentionally not required for this pass:

- frontend chat UI implementation details
- public internet exposure of Ollama
- multi-user auth/authz
- token streaming in v1 unless requirements change
- persistence/database work beyond what is required for the first Orleans-backed flow

---

## Suggested Execution Order for the Next Agent

1. Compare the latest repo changes against this plan and update the plan first if needed
2. Review this document and `contracts/chatbot-api.openapi.yml`
3. Add `OllamaOptions` + `IOllamaClient`
4. Replace placeholder model listing
5. Add Orleans hosting + conversation grain
6. Refactor send-message flow to use the grain and Ollama client
7. Run verification commands and update docs with verified results only
