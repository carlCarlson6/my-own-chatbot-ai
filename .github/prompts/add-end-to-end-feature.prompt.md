---
description: "Use when implementing a complete feature across the OpenAPI contract, backend endpoint, and frontend UI."
name: "Add End-to-End Feature"
---

You are implementing a complete end-to-end feature for `my-own-chatbot-ai`. Follow every step in order. Do NOT skip steps or reorder them.

## Step 1 — Start from the contract

1. Open `contracts/chatbot-api.openapi.yml`.
2. Check whether the feature already has an endpoint defined.
   - If **not**, add the endpoint to the contract first before touching any code.
   - Define request and response schemas with explicit field names, types, and nullable behavior.
   - Follow the naming conventions already in the file (e.g., `SendMessageRequest`, `SendMessageResponse`, `ApiError`).
3. If the endpoint already exists, confirm its shape still matches what you are about to implement.
4. The contract is the source of truth — the backend and frontend must conform to it, not the other way around.

## Step 2 — Backend implementation

1. Locate or create the correct feature slice folder under `backend/src/MyOwnChatbotAi.Api/` (e.g., `Conversations/SendMessage/`).
2. Add or update the Minimal API endpoint file for this feature.
   - Keep it thin: request parsing → validation → Orleans grain or service call → response shaping.
   - Do not put business logic inside the endpoint file.
3. If the feature involves conversation or session state, route through the relevant Orleans grain identity.
4. Confirm the response shape — field names, types, status codes — matches the OpenAPI contract exactly.
5. Run `dotnet build backend/src/MyOwnChatbotAi.sln` and fix any errors before continuing.

## Step 3 — Frontend implementation

1. Add or update the Zod schema that mirrors the new or updated contract shape.
   - Place schemas alongside their feature area or in a shared `schemas/` module.
   - Mirror every field, type, and optional/nullable marker from the OpenAPI spec.
2. Update the Zustand store only if new shared state is required for this feature.
3. Add or update the UI component(s) that exercise the feature.
   - Keep API call logic (fetch, parse, error handling) separated from rendering.
   - Parse every API response through the Zod schema before storing or displaying it.
   - Implement explicit loading, error, and success states in the component.
4. Run `cd frontend && npm run build` and fix any errors before continuing.
5. Run `cd frontend && npm run lint` and fix any reported issues.

## Step 4 — Documentation updates

1. If you introduced or verified new build/run/test commands, add them to `README.md` under the appropriate section.
2. If `docs/` contains a planning document with steps related to this feature, update the status of those steps.
3. If any verified commands or folder structures changed, keep `/.github/copilot-instructions.md` in sync.

## Step 5 — Verification before finishing

Run all three checks. The feature is **not done** until every check passes with zero errors.

```bash
dotnet build backend/src/MyOwnChatbotAi.sln
cd frontend && npm run build
cd frontend && npm run lint
```

Report the output of each command. If any command fails, fix the issue and re-run before declaring the feature complete.
