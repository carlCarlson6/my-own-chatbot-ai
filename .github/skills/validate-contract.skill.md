---
description: "Validate that backend DTOs and frontend Zod schemas match contracts/chatbot-api.openapi.yml. Reports field-level drift: missing fields, type mismatches, nullability differences. Run before a PR or after touching the contract."
name: "validate-contract"
---

# validate-contract

Compare `contracts/chatbot-api.openapi.yml` against the backend C# DTOs and frontend Zod schemas. Report every drift — missing fields, wrong types, nullability mismatches. The contract is always the source of truth.

## Step 1 — Read the contract

Read `contracts/chatbot-api.openapi.yml` in full. For each operation, note:
- **Path** and **HTTP method**
- **Request body schema** (field names, types, required/optional)
- **Response schemas** per status code (field names, types, required/optional)
- **Error envelope shape** (e.g. `ApiError`)

Build a mental map: `operation → request schema → response schema`.

## Step 2 — Read the backend DTOs

For each operation in the contract, find the corresponding C# files in `backend/src/MyOwnChatbotAi.Api/`:
- `*Request.cs` — maps to the request body schema
- `*Response.cs` — maps to the success response schema

Check each DTO against the contract:

| Check | What to verify |
|---|---|
| Field names | C# property name (camelCase in JSON via serializer) matches OpenAPI field name exactly |
| Field types | C# type maps correctly to OpenAPI type (e.g. `string` → `string`, `int` → `integer`, `bool` → `boolean`) |
| Nullability | Optional OpenAPI fields have `?` or `[JsonIgnore]` equivalents; required fields are non-nullable |
| Extra fields | C# DTO does not have fields absent from the OpenAPI schema |
| Missing fields | C# DTO is not missing fields present in the OpenAPI schema |

## Step 3 — Read the frontend Zod schemas

For each operation in the contract, find the corresponding Zod schema in `frontend/src/`:

Check each schema against the contract:

| Check | What to verify |
|---|---|
| Field names | Zod field key matches OpenAPI field name exactly |
| Field types | Zod type matches OpenAPI type (e.g. `z.string()`, `z.number()`, `z.boolean()`) |
| Nullability | Optional fields use `.optional()` or `.nullable()` as appropriate |
| Extra fields | Schema does not define fields absent from the contract |
| Missing fields | Schema is not missing fields present in the contract |
| Parse usage | API responses are parsed through the schema before being stored or rendered |

## Step 4 — Report drift

For each discrepancy found, report:

```
Operation:  POST /api/conversations/{id}/messages
Layer:      Backend DTO / Frontend Zod
File:       path/to/file
Field:      assistantMessage
Issue:      Missing in backend DTO — present in OpenAPI response schema
Fix:        Add `public string AssistantMessage { get; init; }` to SendMessageResponse.cs
```

Group by severity:
- **Breaking** — field missing entirely, or type is wrong (will cause runtime errors or failed Zod parses)
- **Warning** — nullability mismatch (may cause null reference errors or unexpected `.optional()` failures)
- **Info** — extra field in DTO not in contract (harmless but adds noise)

## Step 5 — Summary

```
Contract validated against:
  Backend DTOs:       X files checked
  Frontend schemas:   X files checked

Drift found:
  Breaking:   N
  Warnings:   N
  Info:       N

Files to fix:
  - path/to/file.cs — <description>
  - path/to/schema.ts — <description>
```

If no drift: "Contract is consistent across backend DTOs and frontend Zod schemas."

## Note

This skill **reports drift only** — it does not modify files. To apply fixes, use the `isabel` agent.
