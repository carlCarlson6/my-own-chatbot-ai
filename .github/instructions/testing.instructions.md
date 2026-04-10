---
description: "Use when writing, running, or verifying tests for the chatbot app backend, frontend, or integration points."
name: "Testing Guidelines"
applyTo: "{backend,frontend}/**/*.{cs,ts,tsx}"
---
# Testing Guidelines

## Verification Before Completion

> **Use the `verify-build` skill** to run the full build and lint suite before claiming any task is done. Do not report success based on code review alone.

- Always run build and test commands before claiming a task is done.
- Do **not** report success unless commands have been run and output confirms no errors.
- If commands or test infrastructure changed, update `README.md` and `/.github/copilot-instructions.md`.

## Contract-First Testing Alignment

- Treat `contracts/chatbot-api.openapi.yml` as the source of truth for all API shapes.
- When adding a new endpoint, verify it matches the contract before writing any tests.
- If you change a contract shape (endpoint path, request/response field, status code), update **all three** together:
  1. Backend request/response types
  2. Frontend Zod schemas
  3. Related tests

## Backend Testing Approach

- Prefer **integration tests** that exercise real HTTP routes over unit tests that mock everything.
- Use `WebApplicationFactory<TProgram>` or an equivalent in-process test host to test endpoint behavior end-to-end.
- Keep unit tests small and focused on pure logic; reserve them for domain helpers, validators, or utilities.
- Use **xUnit** as the test framework when test projects are added — it is the .NET ecosystem standard.
- Mirror the **vertical slice feature layout** of the main project in the test project structure.
  - Example: `Tests/Conversations/SendMessageTests.cs` for the `Conversations/SendMessage` feature slice.
- Keep test arrange/act/assert sections clearly separated and readable.

## Frontend Testing Approach

- Use **type checking and lint** as the first and always-required validation gate before any test run.
- When UI tests exist (e.g., **Vitest** + React Testing Library), test real user flows: render, interact, assert visible output.
- Avoid testing implementation details such as internal state or private hooks directly.
- Validate **Zod schemas** against actual API response shapes to catch contract drift early.
- Keep component tests self-contained; avoid network calls in unit/component tests — use typed mocks or MSW.

## Orleans and Async Flows

- Test grain behavior through the **Orleans test cluster** (`Microsoft.Orleans.TestingHost`) when possible.
- Do **not** use `.Result` or `.Wait()` in tests — keep async all the way with `async Task` test methods.
- Test grain state transitions explicitly: idle → processing → complete/error.
- Isolate Ollama client calls behind an interface so grain tests can substitute a predictable fake.

## General Habits

- Prefer testing **real behavior** over mocking every dependency.
- Tests must be **self-contained**: no manual setup beyond environment prerequisites (e.g., a running Ollama instance for integration tests against the real LLM).
- Document test prerequisites clearly in comments or a test README when they exist.
- Name tests to describe the behavior under test, not the method name.
  - Prefer: `SendMessage_WithEmptyBody_Returns400`
  - Avoid: `TestSendMessage`
- Keep test data minimal and representative; avoid large fixtures unless the scenario requires them.

## Future Test Infrastructure

- Once the **.NET test project** is added and `dotnet test` is verified, update this file and `README.md` with the confirmed command.
- Once **frontend tests** (e.g., Vitest) are added, update this file and `README.md` with the verified `npm run test` or equivalent command.
- Keep `README.md` and `/.github/copilot-instructions.md` in sync whenever new verified test commands are confirmed working.
