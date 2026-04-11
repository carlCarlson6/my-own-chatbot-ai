---
description: "Scaffold a contract-first endpoint change for my-own-chatbot-ai by driving the work from OpenAPI to backend slice to frontend Zod/api wiring."
name: "scaffold-contract-slice"
---

# scaffold-contract-slice

Use this skill when adding or reshaping a **single API endpoint** in `my-own-chatbot-ai` and you want a deterministic contract-first workflow that touches all three layers in the right order.

## Goal

Produce a complete endpoint implementation plan or implementation pass that starts from `contracts/chatbot-api.openapi.yml`, then wires the backend Minimal API slice and frontend Zod/api client updates without drifting from the published contract.

## When to use it

- Adding a new conversation or models endpoint
- Changing a request/response shape for one endpoint
- Moving an endpoint from placeholder behavior to a fully wired implementation
- Auditing a single endpoint for contract drift before implementation

Use `validate-contract` when you only need **drift reporting**. Use this skill when you need the **execution workflow** for the endpoint itself.

## Required repo reads before acting

1. `README.md`
2. `contracts/chatbot-api.openapi.yml`
3. The closest backend feature slice under `backend/src/MyOwnChatbotAi.Api/`
4. The relevant frontend schema/client/store files under `frontend/src/`
5. Any applicable plan in `docs/plans/`

## Workflow

### 1. Define the endpoint contract first

Update `contracts/chatbot-api.openapi.yml` before touching backend or frontend code.

For the target operation, define:
- HTTP method and path
- operation summary/description
- request schema
- success response schema and status code
- error responses (`400`, `500`, and `404` when appropriate)
- examples when they clarify the shape

Keep DTO names explicit and stable, for example `RenameConversationRequest` and `RenameConversationResponse`.

### 2. Scaffold the backend slice

Create or update the matching vertical slice under:

```text
backend/src/MyOwnChatbotAi.Api/<Feature>/<OperationName>/
```

Expected files:
- `<OperationName>Endpoint.cs`
- `<OperationName>Request.cs` when the endpoint has a body
- `<OperationName>Response.cs` when the endpoint returns a body

Backend rules:
- Keep the endpoint thin: validation, mapping, grain/service call, response shaping
- Register the endpoint in `Program.cs`
- If Orleans changes are needed, update the relevant grain interface and implementation
- New grain contract types need `[GenerateSerializer]` and `[Id]` attributes
- Do not place `CancellationToken` in grain interface method signatures

### 3. Scaffold the frontend transport layer

Update the matching frontend transport boundary:
- Zod request/response schemas
- exported TypeScript types
- typed API client function
- store wiring or UI call site if the endpoint is user-facing

Frontend rules:
- Parse responses through Zod before storing or rendering
- Keep transport types separate from UI-only view models
- Do not bypass the backend; the frontend never talks to Ollama directly

### 4. Validate alignment before claiming success

Run this sequence:

1. Invoke `validate-contract` to check DTO/schema drift
2. Invoke `verify-build` to run backend build, frontend build, and frontend lint

If either step reports drift or failures, fix those before finishing.

## Output checklist

Before the work is done, confirm:

- `contracts/chatbot-api.openapi.yml` is updated first
- Backend DTO names and field shapes match the contract exactly
- Frontend Zod schemas match the contract exactly
- The endpoint is registered and reachable through the intended route
- `validate-contract` reports no drift
- `verify-build` passes

## Pairing

- Pair with `new-endpoint.prompt.md` when you want a guided step-by-step task prompt
- Pair with `isabel` when contract changes need coordinated backend/frontend propagation
