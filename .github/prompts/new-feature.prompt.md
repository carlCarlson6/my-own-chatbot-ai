---
description: "Guided end-to-end workflow for adding a new feature to my-own-chatbot-ai. Follows the contract-first, vertical slice, and Orleans orchestration conventions."
name: "Add New Feature"
argument-hint: "Feature name and a brief description, e.g. 'conversation rename — allow users to rename a conversation'"
agent: "agent"
---

Add a new end-to-end feature to `my-own-chatbot-ai`. Follow these steps in order.

Use these references throughout:
- [Workspace instructions](../copilot-instructions.md)
- [API contract](../../contracts/chatbot-api.openapi.yml)
- [Backend instructions](../instructions/backend.instructions.md)
- [Orleans instructions](../instructions/orleans.instructions.md)
- [Frontend instructions](../instructions/frontend.instructions.md)
- [Contract instructions](../instructions/api-contracts.instructions.md)
- [Testing instructions](../instructions/testing.instructions.md)
- [README](../../README.md)

---

## Step 1 — Repo inspection

Before writing a single line:
1. Read `README.md` and `contracts/chatbot-api.openapi.yml`.
2. Identify the closest existing feature slice (e.g. `backend/src/.../Conversations/`) to use as a structural reference.
3. Read the relevant frontend files (`frontend/src/`) to understand current Zustand store shape and Zod schemas.
4. If a planning doc exists in `plans/` for this feature, read it and check its status markers.

---

## Step 2 — Update the OpenAPI contract

1. Open `contracts/chatbot-api.openapi.yml`.
2. Add or update the endpoint(s) for this feature: path, method, request schema, response schema, error responses.
3. Use explicit, stable names (`XxxRequest`, `XxxResponse`).
4. Make nullability and optional behavior explicit.

---

## Step 3 — Backend vertical slice

1. Create a new folder under the correct feature area (e.g. `backend/src/MyOwnChatbotAi.Api/Conversations/RenameConversation/`).
2. Add files:
   - `XxxEndpoint.cs` — Minimal API endpoint, thin (validation + grain call + response)
   - `XxxRequest.cs` — Request DTO matching OpenAPI schema
   - `XxxResponse.cs` — Response DTO matching OpenAPI schema
3. Register the endpoint in `Program.cs`.
4. If this feature requires new grain methods, add them to the relevant grain interface and implement in the grain class.
   - Add `[GenerateSerializer]` / `[Id]` attributes to all new grain contract types.
   - Do not use `CancellationToken` in grain interface method signatures.

---

## Step 4 — Frontend

1. Add or update the Zod schema in `frontend/src/` to mirror the new OpenAPI shapes.
2. Add the API call function (typed, validated through Zod).
3. Update the Zustand store slice for any shared state this feature introduces.
4. Add the React component(s) needed to expose the feature in the UI.
5. Wire the component into the existing chat UI layout.

---

## Step 5 — Verify

Run all of the following and confirm they pass before claiming done:

```bash
dotnet build backend/src/MyOwnChatbotAi.sln
cd frontend && npm run build
cd frontend && npm run lint
```

---

## Step 6 — Commit and push

Stage all changed files and commit:
```
Add <feature name>

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```
Push with `git push`.

---

## Output

Provide:
- **Summary**: what was added and where
- **Files created/modified**: list
- **Verification**: commands run and results
- **Follow-ups**: anything left for future work
