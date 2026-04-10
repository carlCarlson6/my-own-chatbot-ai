---
description: "Review the .github/ Copilot customization setup and produce a prioritized improvement plan. Does NOT apply any changes — creates a plan doc in plans/ for review and approval before execution."
name: "Review Copilot Setup"
argument-hint: "Optional focus area, e.g. 'agents', 'instructions applyTo', 'missing prompts', or leave blank for a full review"
agent: "agent"
---

Review the `.github/` Copilot customization folder and produce a structured improvement plan. **Do not apply any changes.** The output of this workflow is a plan document in `plans/` that can be reviewed, approved, and executed later.

---

## What this workflow does

1. Reads every file in `.github/` to understand the current state
2. Analyses the setup for issues across six categories
3. Writes a dated improvement plan to `plans/copilot-config-improvement-<YYYY-MM-DD>-plan.md`
4. Registers the plan in `README.md`
5. **Stops** — does not implement any change

---

## Step 1 — Read the current .github/ state

Read every file listed below before forming any opinion. Do not skip files.

**Root**
- `.github/copilot-instructions.md`

**Agents** (`.github/agents/`)
- Every `*.agent.md` file present

**Instructions** (`.github/instructions/`)
- Every `*.instructions.md` file present

**Prompts** (`.github/prompts/`)
- Every `*.prompt.md` file present

Also read:
- `README.md` — to understand the current project state and verified commands
- `contracts/chatbot-api.openapi.yml` — to understand the API contract scope
- `plans/` — list existing plans so the new plan does not duplicate them

---

## Step 2 — Analyse across six categories

For each category, list specific findings with the file name and the problem or gap. If a category has no issues, note "No issues found."

### Category 1 — Redundancy
- Does `copilot-instructions.md` repeat content already in instruction files?
- Do any agent files copy conventions verbatim from instruction files?
- Do multiple instruction files cover the same rules for the same file types?

### Category 2 — `applyTo` correctness
- Are any `applyTo` patterns too broad (e.g. `**` or `**/*.cs` when only `backend/**/*.cs` makes sense)?
- Are any `applyTo` patterns too narrow (missing files that genuinely need the instruction)?
- Do multiple instruction files use identical or heavily overlapping `applyTo` patterns, causing double-loading?

### Category 3 — Instruction coverage gaps
- Are there areas of the codebase with no matching instruction file?
- Are any instruction files missing key sections (verification steps, examples, acceptance rules)?
- Are there conventions used in the codebase that are not documented in any instruction file?

### Category 4 — Agent coverage gaps
- What developer workflows exist that have no dedicated agent?
- Are existing agents well-scoped, or do they try to do too much?
- Is there a project-aware code reviewer? A contract-updater? An infra agent?

### Category 5 — Prompt coverage gaps
- What recurring developer tasks have no guided prompt?
- Are existing prompts complete (do they have verification steps, output format, commit instructions)?
- Are there prompts referenced in agent files that do not exist yet?

### Category 6 — Staleness and accuracy
- Do any files reference paths, commands, plans, or features that no longer exist?
- Are verified commands still accurate against the real `package.json`, `.sln`, and `vite.config.*`?
- Do instruction files still match the actual project structure and tech stack?

---

## Step 3 — Score and prioritise findings

For each finding, assign:
- **Impact**: High / Medium / Low
- **Effort**: Small (< 30 min) / Medium (30–90 min) / Large (> 90 min)
- **Category**: one of the six above

Group findings into:
- **Must fix** — High impact regardless of effort
- **Should fix** — Medium impact, Small or Medium effort
- **Nice to have** — Low impact or Large effort

---

## Step 4 — Write the plan document

Create the file at:
```
plans/copilot-config-improvement-<YYYY-MM-DD>-plan.md
```
where `<YYYY-MM-DD>` is today's date.

The plan **must** follow `planning.instructions.md` conventions exactly:
- Include `## Goal`, `## Pre-Implementation Change Review`, at least one `## Phase`, `## Acceptance Criteria`
- Use `⏳ Pending` status markers on all phases (nothing is done yet)
- Include `_Last updated: YYYY-MM-DD_` near the top
- Group phases by priority: Phase 1 = Must fix, Phase 2 = Should fix, Phase 3 = Nice to have
- Each task must be specific and actionable (file name + what to change)

Example phase entry:
```md
### Phase 1 — Fix redundancy and applyTo ⏳ Pending

- [ ] `copilot-instructions.md`: remove "Conventions" section (covered by backend/frontend instructions)
- [ ] `backend.instructions.md`: change applyTo from `**/*.{cs,json}` to `backend/**/*.{cs,json}`
```

---

## Step 5 — Register in README

Add the new plan to the `## Plans` section of `README.md` under `### WIP / In-Progress`:

```md
- [`plans/copilot-config-improvement-<YYYY-MM-DD>-plan.md`](plans/copilot-config-improvement-<YYYY-MM-DD>-plan.md) — Copilot setup review: prioritized .github/ improvements.
```

---

## Step 6 — Stop

**Do not implement any change from the plan.**

The plan is for human review and approval. It will be executed in a future session when approved.

---

## Output

Provide a concise summary:
- **Findings count by category**: e.g. "Redundancy: 2, applyTo: 3, Coverage gaps: 1, Staleness: 1"
- **Plan created at**: file path
- **Top 3 must-fix items**: brief list
- **Next step**: "Review `plans/copilot-config-improvement-<date>-plan.md` and run this plan when ready"
