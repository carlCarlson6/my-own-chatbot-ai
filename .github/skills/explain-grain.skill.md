---
description: "Explain an Orleans grain in my-own-chatbot-ai: its state shape, lifecycle, interface methods, and role in conversation orchestration. Invoke against a specific grain file to understand or document it."
name: "explain-grain"
---

# explain-grain

Given an Orleans grain file (or grain interface), explain it clearly: what state it owns, how it activates and transitions, what each method does, and how it fits into the chatbot's conversation orchestration.

## Step 1 — Read the grain files

Read all of:
- The grain interface file (e.g. `IConversationGrain.cs`)
- The grain implementation file (e.g. `ConversationGrain.cs`)
- The grain state class if it is in a separate file
- Any related DTOs or Orleans serialization types used by the grain

Also read `contracts/chatbot-api.openapi.yml` to understand how the API surface connects to this grain.

## Step 2 — Explain the state

Describe the grain state clearly:

- **Grain key type**: string / Guid / integer key — what does the key represent? (e.g. conversation ID)
- **State fields**: list each field, its type, its purpose, and whether it is append-only or mutable
- **Persistence**: which storage provider is used (`[StorageName("...")]`)? In-memory or durable?
- **Serialization**: are `[GenerateSerializer]` and `[Id(...)]` attributes correctly applied?

## Step 3 — Explain the lifecycle

Map the grain's observable states using the project's standard progression:

```
Not activated → Activated (idle) → Processing → Complete / Error → Deactivated
```

For each transition:
- What triggers it?
- What state changes?
- What side effects occur (e.g. Ollama call, state write)?

If the grain uses reminders or timers, explain when they fire and what they do.

## Step 4 — Explain each interface method

For each method on the grain interface:

| Method | Parameters | Return | Purpose | Side effects |
|---|---|---|---|---|
| `SendMessageAsync` | `string message` | `Task<MessageResponse>` | ... | Calls Ollama, appends to history |

Note any methods that must be called in sequence, or that are unsafe to call concurrently.

## Step 5 — Explain the API integration

- Which Minimal API endpoint(s) resolve this grain?
- How does the endpoint get the grain reference? (`IGrainFactory`, `IClusterClient`)
- What mapping happens between the HTTP request/response and the grain method call?

## Step 6 — Flag any issues

While reading the grain, note:
- Missing `[GenerateSerializer]` / `[Id]` on contract types
- `CancellationToken` in grain interface signatures (not supported by Orleans)
- `.Result` or `.Wait()` calls (must be async end-to-end)
- State fields storing UI-facing DTOs directly (should map to internal types)
- Concurrency risks: multiple callers that could race on the same grain identity

## Output format

```
## Grain: <ClassName>

### Key
<type and meaning>

### State
<field table>

### Lifecycle
<transitions diagram or prose>

### Methods
<method table>

### API wiring
<endpoint → grain path>

### Issues found
<list or "None">
```
