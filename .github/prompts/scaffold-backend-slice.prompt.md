---
description: "Scaffold a new backend vertical slice for the chatbot .NET API using Minimal APIs, Orleans, and the repo’s contract-first conventions."
name: "Scaffold Backend Slice"
argument-hint: "Describe the slice, e.g. SendMessage endpoint for a conversation"
agent: "agent"
---
Create a new backend vertical slice for the feature described in the prompt arguments.

Follow these workspace references:
- [Backend instructions](../instructions/backend.instructions.md)
- [Orleans instructions](../instructions/orleans.instructions.md)
- [API contract instructions](../instructions/api-contracts.instructions.md)
- [Workspace instructions](../copilot-instructions.md)

Requirements:
1. Use a **vertical slice** structure organized by feature or use case.
2. Use **Minimal APIs** with the endpoint defined in its own file.
3. Keep business logic in **Orleans** grains or Orleans-coordinated services.
4. Respect the `contracts/` OpenAPI YAML as the API source of truth.
5. Keep changes minimal and aligned with the current repo state.

When executing this prompt:
- Inspect the current backend structure before creating files.
- If project files are missing, scaffold only the minimal backend pieces needed and clearly note what is still missing.
- Create or update the request/response contract, endpoint file, and Orleans interaction points relevant to the requested slice.
- Run the real `dotnet restore`, `dotnet build`, and `dotnet test` commands only if the .NET project exists, and report the actual output as evidence.

Output:
- A short summary of the requested slice
- The files created or changed
- Any verified commands run and their results
- Any follow-up steps still needed
