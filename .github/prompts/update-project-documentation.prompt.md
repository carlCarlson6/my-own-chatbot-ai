---
description: "Review the repo’s current state and update README/docs/.github customization files so they accurately reflect the project status, architecture, technologies, and working conventions."
name: "Update Project Documentation"
argument-hint: "Optional focus, e.g. after adding Orleans grains, frontend setup, or new architecture decisions"
agent: "agent"
---
Refresh the project documentation for the current workspace based on the **actual repo state**.

Follow these workspace references first:
- [Workspace instructions](../copilot-instructions.md)
- [Backend instructions](../instructions/backend.instructions.md)
- [Frontend instructions](../instructions/frontend.instructions.md)
- [Orleans instructions](../instructions/orleans.instructions.md)
- [API contract instructions](../instructions/api-contracts.instructions.md)
- [Main README](../../README.md)
- [Scaffolding plan](../../docs/scaffolding-plan.md)
- [API contracts folder](../../contracts/README.md)

Task:
1. Inspect the current repo state before editing any documentation.
2. Review the main documentation and customization files, especially:
   - `README.md`
   - `docs/**/*.md`
   - `.github/copilot-instructions.md`
   - `.github/instructions/**/*.md`
   - `.github/prompts/**/*.prompt.md`
   - any relevant contract or architecture docs
3. Update the docs so they better reflect:
   - the current implementation status
   - verified build/run/test commands
   - technologies currently in use
   - new methodologies or patterns adopted in the repo
   - architecture boundaries between backend, frontend, contracts, and AI integration
4. Keep changes minimal, factual, and aligned with the real codebase.
5. Do **not** invent commands, dependencies, or features that are not present.
6. If a stated command or architecture note is outdated, correct it using the current verified repo state.

Rules:
- Prefer updating existing docs over creating new ones unless a new file is clearly needed.
- Use the OpenAPI contract in `contracts/` as the API source of truth.
- Keep frontend/backend responsibilities clearly separated.
- Preserve the repo’s local-first direction with Ollama unless the workspace shows otherwise.
- Apply **verification-before-completion**: if you mention build/test/run success, include the actual command and result.

Output format:
- **Summary:** what changed and why
- **Files updated:** list of files created/edited
- **Verification:** commands actually run and the observed results
- **Follow-ups:** any docs that still need future updates as implementation grows
