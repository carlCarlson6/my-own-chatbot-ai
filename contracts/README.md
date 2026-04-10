# API Contracts

This folder contains the **source of truth** for the chatbot application's API surface.

## Current contract

- `chatbot-api.openapi.yml` — initial MVP contract for:
  - creating a conversation
  - sending a message
  - loading conversation history
  - listing available Ollama models

## Rules

- Update the OpenAPI contract **before** changing backend or frontend implementations.
- Keep request/response names explicit and stable.
- Mirror transport schemas in the frontend with **Zod**.
- Keep backend request/response DTOs aligned with the published contract.

## Current MVP scope

Included now:
- local-first chat flow
- one conversation at a time
- in-memory state
- no authentication
- no streaming yet

Deferred for later:
- persistence/database support
- auth and multi-user features
- token streaming
- deployment and production hardening
