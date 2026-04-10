---
description: "Use when defining or changing frontend/backend API contracts for the chatbot app. Covers OpenAPI YAML as the source of truth, DTO naming, and keeping .NET and React/Zod models aligned."
name: "API Contract Guidelines"
applyTo: "contracts/**/*.yml, contracts/**/*.yaml"
---
# API Contract Guidelines

## Source of Truth

- Treat **OpenAPI YAML** files in `contracts/` as the source of truth for frontend/backend API contracts.
- Prefer a clear canonical file such as `contracts/chatbot-api.openapi.yml`.
- Update the contract first when an endpoint, payload, or error shape changes; then implement backend and frontend changes from that spec.

## DTO and Schema Conventions

- Use explicit request and response names such as `SendMessageRequest`, `SendMessageResponse`, `ConversationHistoryResponse`, and `ApiError`.
- Keep field names stable, descriptive, and easy for frontend Zod schemas to mirror.
- Make nullable and optional behavior explicit in the OpenAPI schema.

## Backend Alignment

- Minimal API request and response types must match the published contract.
- Do not introduce fields, status codes, or error shapes that are missing from the OpenAPI spec.
- Keep validation and serialization behavior consistent with the contract.

## Frontend Alignment

- Frontend **Zod** schemas should closely mirror the OpenAPI contracts.
- Parse API responses against the contract before storing or rendering them.
- Keep transport DTOs separate from UI-only view models when needed.

## API Design Rules

- Define predictable error envelopes and status codes.
- Add useful examples for chat message requests, model selection, and conversation history responses.
- Document streaming or long-running responses explicitly if the chat experience uses them.

## Repo Awareness

- Store contract specifications under `contracts/` rather than scattering them across frontend or backend folders.
- Keep `README.md` and implementation details aligned with the verified contract location and naming.
