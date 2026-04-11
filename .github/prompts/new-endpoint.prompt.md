---
description: "Contract-first workflow for adding a single new API endpoint to my-own-chatbot-ai. Updates OpenAPI spec first, then scaffolds backend slice, then updates frontend types."
name: "Add New Endpoint"
argument-hint: "Endpoint purpose and route, e.g. 'DELETE /api/conversations/{id} — delete a conversation'"
agent: "agent"
---

Add a single new API endpoint to `my-own-chatbot-ai` using a strict contract-first approach.

Use these references:
- [API contract](../../contracts/chatbot-api.openapi.yml)
- [Backend instructions](../instructions/backend.instructions.md)
- [Contract instructions](../instructions/api-contracts.instructions.md)
- [Frontend instructions](../instructions/frontend.instructions.md)

Recommended skills:
- `scaffold-contract-slice` — for the end-to-end contract → backend → frontend scaffolding flow
- `validate-contract` — for final drift checking before completion

---

## Step 1 — Read current contract

Open `contracts/chatbot-api.openapi.yml` and understand:
- Existing endpoint patterns and naming conventions
- Existing error response shapes (reuse them)
- Existing DTO naming patterns

---

## Step 2 — Update the OpenAPI contract

Add the new endpoint to `contracts/chatbot-api.openapi.yml`:
- Path and HTTP method
- Request body schema (if applicable)
- Path/query parameters (if applicable)
- Success response schema and status code
- Error responses (at minimum 400 and 500; add 404 if the resource may not exist)
- A brief `summary` and `description` on the operation

**Do not proceed to Step 3 until the contract is updated.**

---

## Step 3 — Backend vertical slice

Create a new folder under the relevant feature area:
```
backend/src/MyOwnChatbotAi.Api/<Feature>/<OperationName>/
  <OperationName>Endpoint.cs
  <OperationName>Request.cs      (if endpoint has a request body)
  <OperationName>Response.cs     (if endpoint returns a body)
```

Rules:
- Endpoint file is thin: validate input → call grain/service → return response
- DTO field names and types must exactly match the OpenAPI schema
- Register the endpoint in `Program.cs`
- If Orleans grain changes are needed, follow `orleans.instructions.md`

---

## Step 4 — Frontend types

Add or update in `frontend/src/`:
- Zod schema mirroring the new request/response shapes
- TypeScript types derived from the Zod schema
- API call function (typed, response validated through Zod before use)

---

## Step 5 — Verification checklist

Before claiming done, confirm each item:

- [ ] `contracts/chatbot-api.openapi.yml` is updated with the new endpoint
- [ ] Backend DTO field names match the OpenAPI schema exactly
- [ ] Frontend Zod schema mirrors the OpenAPI response shape
- [ ] `dotnet build backend/src/MyOwnChatbotAi.sln` passes
- [ ] `cd frontend && npm run build` passes
- [ ] `cd frontend && npm run lint` passes

---

## Step 6 — Commit and push

```
Add <endpoint description>

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```
