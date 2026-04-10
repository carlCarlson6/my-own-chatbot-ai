---
description: "Guided workflow to add the first test infrastructure to my-own-chatbot-ai: xUnit backend integration tests and Vitest frontend component tests."
name: "Setup Tests"
argument-hint: "Optional focus: 'backend only', 'frontend only', or leave blank for both"
agent: "agent"
---

Add test infrastructure to `my-own-chatbot-ai` following the project's testing conventions.

Use these references:
- [Testing instructions](../instructions/testing.instructions.md)
- [Backend instructions](../instructions/backend.instructions.md)
- [Frontend instructions](../instructions/frontend.instructions.md)
- [README](../../README.md)

---

## Step 1 — Repo inspection

Before creating any files:
1. Read `README.md` to confirm the current verified commands.
2. Check `backend/src/MyOwnChatbotAi.sln` for existing test projects.
3. Check `frontend/package.json` for existing test scripts (`vitest`, `@testing-library/react`, etc.).
4. Do not create test infrastructure that already exists.

---

## Backend — xUnit integration tests

### Create the test project

```bash
dotnet new xunit -n MyOwnChatbotAi.Tests -o backend/src/MyOwnChatbotAi.Tests
dotnet sln backend/src/MyOwnChatbotAi.sln add backend/src/MyOwnChatbotAi.Tests/MyOwnChatbotAi.Tests.csproj
```

### Add required packages

```bash
cd backend/src/MyOwnChatbotAi.Tests
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

### Structure

Mirror the vertical slice layout of the main project:
```
backend/src/MyOwnChatbotAi.Tests/
  Conversations/
    GetConversationTests.cs
    SendMessageTests.cs
  Shared/
    TestWebAppFactory.cs   ← WebApplicationFactory<Program> setup
```

### First test

Write one real integration test that exercises an existing HTTP route end-to-end using `WebApplicationFactory<Program>`. Name it to describe behavior:
```
GetConversation_WhenNotFound_Returns404
```

### Verify backend tests

```bash
dotnet test backend/src/MyOwnChatbotAi.sln
```

Confirm the output shows the test running and passing.

---

## Frontend — Vitest + React Testing Library

### Install dependencies

```bash
cd frontend
npm install --save-dev vitest @testing-library/react @testing-library/jest-dom @testing-library/user-event jsdom
```

### Configure Vitest

Add to `frontend/vite.config.ts`:
```ts
test: {
  environment: 'jsdom',
  setupFiles: ['./src/test/setup.ts'],
  globals: true,
}
```

Create `frontend/src/test/setup.ts`:
```ts
import '@testing-library/jest-dom';
```

Add to `frontend/package.json` scripts:
```json
"test": "vitest run",
"test:watch": "vitest"
```

### First test

Write one component test that renders a real component and asserts visible output:
```
frontend/src/components/__tests__/ChatMessage.test.tsx
```

### Verify frontend tests

```bash
cd frontend && npm run test
```

Confirm the test runs and passes.

---

## Step 2 — Update documentation

After both pass:

1. Update `README.md` — add verified test commands under the Build and Test section.
2. Update `.github/instructions/testing.instructions.md` — replace "Future Test Infrastructure" notes with the verified commands.
3. Update `.github/copilot-instructions.md` — add backend test command to the Verified Commands table.

---

## Step 3 — Commit and push

```
Add xUnit and Vitest test infrastructure

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

---

## Output

Provide:
- **Files created**: list
- **Verification**: commands run and results
- **README changes**: summary of what was updated
