---
description: "Run the full verification suite for my-own-chatbot-ai and report pass/fail. Executes backend build, frontend build, and frontend lint. Invoke at the end of any task before claiming success."
name: "verify-build"
---

# verify-build

Run the full verification suite and confirm everything passes before claiming a task is done. Report actual command output — do not assume success.

## Run all checks

Execute in order. Do not stop on first failure — run all three and report each result.

### 1. Backend build

```bash
dotnet build backend/src/MyOwnChatbotAi.sln
```

✅ Pass: "Build succeeded." in output  
❌ Fail: report the first compiler error with file and line

### 2. Frontend build

```bash
cd frontend && npm run build
```

✅ Pass: Vite build completes with no errors  
❌ Fail: report the TypeScript or bundler error

### 3. Frontend lint

```bash
cd frontend && npm run lint
```

✅ Pass: ESLint exits with 0 warnings/errors  
❌ Fail: report the lint violations

## If backend tests exist

```bash
dotnet test backend/src/MyOwnChatbotAi.sln
```

Run this only if a test project is present in the solution. Check `backend/src/` for any `*.Tests.csproj` before running.

## If frontend tests exist

```bash
cd frontend && npm run test
```

Run this only if a `test` script exists in `frontend/package.json`.

## Output

```
Backend build:   ✅ succeeded  |  ❌ failed — <error>
Frontend build:  ✅ succeeded  |  ❌ failed — <error>
Frontend lint:   ✅ passed     |  ❌ failed — <violation count> violations
Backend tests:   ✅ passed / ❌ failed / ⏭ skipped (no test project)
Frontend tests:  ✅ passed / ❌ failed / ⏭ skipped (no test script)
```

**Do not report a task as done unless all applicable checks show ✅.**
