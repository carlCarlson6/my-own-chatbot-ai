---
description: "Use when creating or updating implementation, execution, scaffolding, or workflow plans in repo Markdown docs. Requires a pre-implementation change review/status step so the next agent checks for repo drift before acting."
name: "Planning Document Guidelines"
applyTo: "docs/**/*.md"
---
# Planning Document Guidelines

## Location rule

- Every new plan document **must** be created under `docs/`.
- Never create plan files in the repo root, under `backend/`, `frontend/`, or anywhere else.

## Naming rule

- Use kebab-case file names ending in `-plan.md`.
- Examples: `frontend-chat-ui-plan.md`, `token-streaming-plan.md`.
- Do not use generic names like `plan.md` or `next-steps.md`.

## README registration rule

- Every new plan **must** be added to the `## Plans` section of `README.md` immediately after the file is created.
- Add it under `### WIP / In-Progress` with a markdown link and a one-line description.
- This is a **required step** — not optional and not deferrable.

Example entry:
```md
- [`docs/frontend-chat-ui-plan.md`](docs/frontend-chat-ui-plan.md) — Frontend chat UI: wire send-message flow to live backend.
```

## Required sections rule

Every plan document **must** include all of the following sections:

| Section | Notes |
|---|---|
| `## Goal` or `## Purpose` | What this plan achieves and why |
| `## Pre-Implementation Change Review` | Blocking gate — see rule below |
| At least one `## Phase …` or step breakdown | Structured list of implementation phases or tasks |
| `## Acceptance Criteria` | Explicit, verifiable conditions that define "done" |
| `_Last updated: YYYY-MM-DD_` | Near the top of the document |

## Status marker rule

- Use `✅ Done` and `⏳ Pending` in phase/section headings to show current progress.
- Update these markers as work progresses — do not leave stale status.
- Example heading: `### Phase 2 — Add Ollama client layer ✅ Done`

## Completion rule

When all phases of a plan are marked `✅ Done`:
1. Move the plan's `README.md` entry from `### WIP / In-Progress` to `### Completed`.
2. Update the entry's one-line description to note it is fully done.
3. Update the plan file's `_Last updated:` marker with the completion date.

## Pre-implementation gate rule

The `## Pre-Implementation Change Review` section is a **blocking gate** — not informational text.

- An agent **must** read and validate the current repo state against the plan before writing any code.
- Required review steps before coding:
  1. Read `README.md`, `contracts/chatbot-api.openapi.yml`, and all affected `backend/` or `frontend/` paths.
  2. Compare the current repo against the plan: check for drift in endpoints, DTOs, service registrations, file structure, status markers, and dependencies.
  3. If the plan is stale or any step is already done, **update the plan first**, then proceed.
  4. Only after a passing review should implementation begin.

Use wording similar to this in every plan:

```md
## Pre-Implementation Change Review

Before starting implementation work, first inspect the current repo state and confirm this plan still matches reality. If commands, structure, status, or acceptance criteria changed, refresh the plan before coding.

### Required review steps
1. Read `README.md`, `contracts/chatbot-api.openapi.yml`, and the affected implementation folders.
2. Compare the current repo against this plan: endpoints, DTOs, service registrations, status markers.
3. Update this plan first if any step is stale or already completed.
4. Only after that review should implementation continue.
```

## Plan template

Use this as a starting point for new plans:

```md
# <Feature Name> Plan

_Last updated: YYYY-MM-DD_

## Goal

<One-paragraph description of what this plan achieves and why.>

---

## Pre-Implementation Change Review

Before starting implementation work, first inspect the current repo state and confirm this plan still matches reality. If commands, structure, status, or acceptance criteria changed, refresh the plan before coding.

### Required review steps
1. Read `README.md`, `contracts/chatbot-api.openapi.yml`, and the affected implementation folders.
2. Compare the current repo against this plan: endpoints, DTOs, service registrations, status markers.
3. Update this plan first if any step is stale or already completed.
4. Only after that review should implementation continue.

---

## Phase 1 — <Phase name> ⏳ Pending

### Tasks
- [ ] Task one
- [ ] Task two

### Files
- `path/to/file.cs`

---

## Acceptance Criteria

- [ ] Criterion one
- [ ] Criterion two

---

## Out of Scope

- Item not covered by this plan
```

## Keep plans current

- Keep the `## Pre-Implementation Change Review` section close to the top so future agents see it before detailed phases.
- Refresh verification steps and acceptance criteria whenever the repo evolves.
- Do not leave completed tasks marked `⏳ Pending` — update status markers as work progresses.
