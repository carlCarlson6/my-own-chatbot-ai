---
description: "Scaffold a new plan document in docs/plans/ from the canonical template. Pre-fills today's date, required sections, and Pending status markers. Registers the plan in README automatically."
name: "scaffold-plan"
---

# scaffold-plan

Create a new plan document in `docs/plans/` using the canonical template. Pre-fill the structure so nothing is forgotten. Register it in `README.md` immediately.

## Step 1 — Determine the plan name and goal

You need:
- A **kebab-case name** for the feature or task (e.g. `conversation-rename`, `token-streaming`)
- A **one-sentence goal** describing what the plan achieves

The file will be created at:
```
docs/plans/<kebab-name>-plan.md
```

## Step 2 — Create the plan file

Use today's date for `_Last updated:_`. All phases start as `⏳ Pending`.

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
- `path/to/file`

---

## Acceptance Criteria

- [ ] Criterion one
- [ ] Criterion two

---

## Out of Scope

- Item not covered by this plan
```

## Step 3 — Register in README

Add to the `## Plans` section under `### WIP / In-Progress`:

```md
- [`docs/plans/<kebab-name>-plan.md`](docs/plans/<kebab-name>-plan.md) — <one-line description>.
```

If the `## Plans` section or `### WIP / In-Progress` subsection does not exist yet, create them.

## Step 4 — Confirm

Report:
- **Plan created**: `docs/plans/<kebab-name>-plan.md`
- **README updated**: yes/no
- **Next step**: fill in the phases and tasks, then use the `chatbot-builder` agent to execute
