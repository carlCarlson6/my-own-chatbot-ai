# Copilot Workflow Improvement Plan

## Purpose

This document captures a practical plan for improving the GitHub Copilot workflow in `my-own-chatbot-ai`.
It is written so a future agent can implement the changes incrementally without needing to rediscover the decisions.

---

## Current State

The repository already has a solid baseline:

- `/.github/copilot-instructions.md`
- `/.github/instructions/api-contracts.instructions.md`
- `/.github/instructions/backend.instructions.md`
- `/.github/instructions/frontend.instructions.md`
- `/.github/instructions/orleans.instructions.md`
- `/.github/prompts/scaffold-backend-slice.prompt.md`
- `/.github/prompts/update-project-documentation.prompt.md`

The current setup is good for guidance, but there are a few remaining gaps:

1. The main repo guidance is aligned now, but it still needs to stay in sync as the repo evolves.
2. There is no dedicated Copilot guidance for local dev setup and environment issues.
3. There is no testing-focused instruction file yet.
4. There is no repo-specific custom agent.
5. There are not yet enough repeated workflows to justify investing heavily in skills.

---

## Pre-Implementation Change Review

Before implementing recommendations from this plan, first re-check the current repo state so the workflow guidance stays aligned with reality.

### Required review steps

1. Inspect `README.md`, `/.github/`, `docs/`, and any newly added prompt/agent/instruction files.
2. Compare the repo's current Copilot setup against this plan and note any completed, renamed, or superseded items.
3. Update this document first if priorities, file lists, or status notes have drifted.
4. Only after that review should the implementation work begin.

## Main Recommendations

### Do now

- **Keep the current instructions synchronized** as verified commands and repo structure change.
- **Add more reusable prompts** for repeated project workflows.
- **Create one custom repo agent** to keep Copilot behavior focused and consistent.

### Do later

- **Create skills only after patterns become repetitive and stable.**
- Avoid creating many agents or many skills too early.

---

## Decision: Skills vs Agents

### Skills

**Recommendation:** not a priority yet.

Use skills later when the project has repeated multi-step workflows such as:

- OpenAPI contract -> backend endpoint -> frontend/Zod sync
- Orleans grain scaffolding and review
- Ollama integration debugging and verification

### Agents

**Recommendation:** yes, create **one** custom agent now.

Suggested name:

- `chatbot-builder.agent.md`

Suggested responsibilities:

- inspect the repo before making assumptions
- follow the files in `/.github/instructions/`
- avoid inventing commands or structure
- keep backend/frontend/contracts concerns separated
- verify real commands before claiming success
- prefer minimal, project-aligned changes

---

## Implementation Plan

## Phase 1 — Align existing guidance

### Goal
Keep the current Copilot instructions trustworthy and synchronized as the repository evolves.

### Tasks

1. Review `/.github/copilot-instructions.md` and `README.md` whenever backend/frontend structure or verified commands change.
2. Ensure `README.md` and `/.github/copilot-instructions.md` do not contradict each other.
3. Keep instruction scope specific and avoid unnecessarily broad `applyTo` patterns.

### Expected outcome
Copilot responses become more accurate and less likely to rely on outdated assumptions.

---

## Phase 2 — Add missing instruction files

### Goal
Cover the workflow gaps that are likely to recur during development.

### New files to add

#### 1. `/.github/instructions/dev-setup.instructions.md`
Use this for local development setup and troubleshooting.

Should cover:

- required .NET SDK expectations
- local Node/npm setup notes
- the known npm/Homebrew/ICU workaround if still relevant
- local Ollama setup expectations
- reminder to verify real tools before suggesting commands

#### 2. `/.github/instructions/testing.instructions.md`
Use this once tests begin to exist.

Should cover:

- verification-before-completion expectations
- backend/API/frontend validation habits
- how to keep tests aligned with contract-first development
- preference for testing real behavior over mock-only behavior

#### 3. Optional: `/.github/instructions/ollama-integration.instructions.md`
Create this once the backend begins real Ollama orchestration.

Should cover:

- backend-only integration boundary
- timeout/retry/error-handling expectations
- safe DTO mapping
- Orleans + Ollama responsibilities

---

## Phase 3 — Add high-value prompts

### Goal
Make repeated tasks faster and more consistent with slash-command style workflows.

### New prompts to add

#### 1. `/.github/prompts/scaffold-frontend-app.prompt.md`
For bootstrapping the frontend in the project style.

Should guide Copilot to:

- inspect current frontend state first
- scaffold Vite + React + TypeScript structure
- respect Tailwind, Zustand, and Zod conventions
- keep alignment with the OpenAPI contract

#### 2. `/.github/prompts/add-end-to-end-feature.prompt.md`
For repeated feature work across the stack.

Should guide Copilot to:

- start from `contracts/chatbot-api.openapi.yml`
- update backend endpoint/contracts
- align frontend validation and UI types
- update docs if needed
- verify commands before reporting completion

#### 3. Optional: `/.github/prompts/check-contract-drift.prompt.md`
For auditing drift between:

- OpenAPI contract
- backend request/response models
- frontend Zod schemas

---

## Phase 4 — Create one custom agent

### Goal
Give the project a reusable repo-aware implementation mode.

### File to add

- `/.github/agents/chatbot-builder.agent.md`

### Agent behavior

The agent should:

1. inspect the workspace before changing files
2. use the repo instructions as the primary source of guidance
3. keep changes minimal and consistent with the project architecture
4. respect contract-first development
5. verify real build/test commands before claiming success
6. prefer local-first choices and backend-mediated Ollama integration

### Follow-up
After the agent exists, update relevant prompt files so they can target that named agent instead of a generic one.

---

## Phase 5 — Revisit skills later

### Goal
Only invest in skills when they clearly reduce repeated work.

### Criteria for creating the first skill

Create a skill only if the same multi-step task has appeared several times and the process is stable.

Good future candidates:

- `end-to-end-feature-delivery` skill
- `orleans-grain-implementation` skill
- `ollama-debugging` skill

### Non-goal for now
Do **not** create many narrow skills early in the project.
That would add maintenance burden without much workflow benefit.

---

## Recommended Future `.github` Structure

```text
.github/
  copilot-instructions.md
  instructions/
    api-contracts.instructions.md
    backend.instructions.md
    frontend.instructions.md
    orleans.instructions.md
    dev-setup.instructions.md              # new
    testing.instructions.md                # new
    ollama-integration.instructions.md     # optional later
  prompts/
    scaffold-backend-slice.prompt.md
    update-project-documentation.prompt.md
    scaffold-frontend-app.prompt.md        # new
    add-end-to-end-feature.prompt.md       # new
    check-contract-drift.prompt.md         # optional later
  agents/
    chatbot-builder.agent.md               # new
```

---

## Recommended Priority Order

1. **Update `copilot-instructions.md`**
2. **Add `dev-setup.instructions.md`**
3. **Add `testing.instructions.md`**
4. **Add `scaffold-frontend-app.prompt.md`**
5. **Add `add-end-to-end-feature.prompt.md`**
6. **Create `chatbot-builder.agent.md`**
7. Reassess whether skills are justified

---

## Acceptance Criteria for the Future Agent

This plan can be considered successfully implemented when:

- `README.md` and `/.github/copilot-instructions.md` are aligned
- new instructions exist for setup/testing (and optionally Ollama integration)
- at least one frontend-focused and one end-to-end prompt exist
- one repo-specific agent exists and is usable
- prompts/agent clearly reference the existing architecture and conventions
- no new documentation invents unsupported commands or project structure

---

## Final Recommendation

For this project, the best workflow improvement path is:

- **strengthen the existing instructions**
- **add a few targeted prompts**
- **create one repo-aware agent**
- **delay skills until the repeated workflows are clearer**

This keeps the Copilot setup lightweight, useful, and maintainable while the project is still evolving.
