---
description: "Use when designing Orleans grains, conversation state, reminders, or concurrency-sensitive workflows in the chatbot backend. Covers grain boundaries, state ownership, and safe Ollama orchestration."
name: "Orleans Grain Guidelines"
applyTo: "backend/**/*.cs"
---
# Orleans Grain Guidelines

> These rules govern **grain design only** — API endpoint patterns and Minimal API conventions live in `backend.instructions.md`.

## Grain Boundaries

- Model grains around stable identities such as a **conversation**, **chat session**, or another clear domain unit.
- Use one grain for one clear responsibility; avoid large "god grains".
- Prefer domain-focused names such as `ConversationGrain` or `ChatSessionGrain`.

## State Ownership

- Let the grain own conversation-specific mutable state.
- Keep state explicit: conversation id, ordered messages, selected model, timestamps, and current processing status.
- Do not store UI-facing DTOs directly as grain state; map them to internal state models.

## Concurrency and Safety

- Treat the grain as the primary concurrency boundary for a conversation.
- Route send-message, regenerate, and cancel flows through the same grain identity to avoid race conditions.
- Avoid static mutable state and ad hoc locking outside Orleans unless there is a proven need.
- Use `async Task` end-to-end — never use `.Result` or `.Wait()` anywhere in grain code.

## I/O and Integration

- Keep external **Ollama** calls in dedicated client services coordinated by the grain.
- Keep retries, timeouts, and failure handling explicit and predictable.
- Isolate Ollama-specific client code behind `IOllamaClient` so the model provider can be swapped without touching grain code.

## API Integration

- Minimal API endpoints should resolve the relevant grain and delegate work.
- Keep endpoint files thin: validation, mapping, grain call, and response shaping only.
- Return predictable results for pending, success, and error states.

## Persistence and Evolution

- Persist only what is needed to reconstruct conversation context.
- Version grain state carefully when schemas evolve.
- Prefer append-oriented message history unless the feature explicitly needs edits or rewrites.

## Verification Before Completion

- Once the .NET solution exists, verify Orleans-related changes with the repo’s real **restore**, **build**, and **test** commands before claiming success.
