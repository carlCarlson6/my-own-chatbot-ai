---
description: "Use when creating or updating implementation, execution, scaffolding, or workflow plans in repo Markdown docs. Requires a pre-implementation change review/status step so the next agent checks for repo drift before acting."
name: "Planning Document Guidelines"
applyTo: "docs/**/*.md"
---
# Planning Document Guidelines

## Required review gate

- Every new or updated plan must include a `## Pre-Implementation Change Review` section near the top of the document.
- Treat this as a required status gate before any coding or execution steps begin.
- The purpose is to catch repo drift when files, commands, contracts, or architecture may have changed since the plan was last updated.

## What that section should instruct

1. Review the latest relevant project state first, such as `README.md`, related files in `docs/`, contracts, and the affected implementation folders.
2. Compare the current repo state against the plan and look for changes in endpoints, DTOs, configuration, dependencies, status markers, or file structure.
3. Update the plan first if any part of it is stale, incomplete, or already superseded by newer work.
4. Only after that review should implementation continue.

## Recommended pattern

Use wording similar to this:

```md
## Pre-Implementation Change Review

Before starting implementation work, first inspect the current repo state and confirm this plan still matches reality. If commands, structure, status, or acceptance criteria changed, refresh the plan before coding.
```

## Keep plans current

- Prefer a `_Last updated: YYYY-MM-DD_` marker when the document is acting as an execution handoff.
- Keep the review section close to the top so future agents see it before detailed phases and task lists.
- Refresh verification steps and acceptance criteria whenever the repo evolves.
