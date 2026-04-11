---
description: "Review the .github/ Copilot customization setup and produce a prioritized improvement plan. Does NOT apply any changes — creates a plan doc in docs/plans/ for review and approval before execution."
name: "Review Copilot Setup"
argument-hint: "Optional focus area, e.g. 'agents', 'instructions applyTo', 'missing prompts', or leave blank for a full review"
agent: "agent"
---

Review the `.github/` Copilot customization folder and produce a structured improvement plan. **Do not apply any changes.** The output of this workflow is a plan document in `docs/plans/` that can be reviewed, approved, and executed later.

---

## What this workflow does

1. Reads every file in `.github/` to understand the current state
2. Analyses the setup for issues across eight categories
3. Writes a dated improvement plan to `docs/plans/copilot-config-improvement-<YYYY-MM-DD>-plan.md`
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

**Skills** (`.github/skills/` — may not exist yet)
- Every `*.skill.md` or `*.yml` file present, if the folder exists
- Note if the folder is absent entirely

**Hooks** (`.github/hooks/` — may not exist yet)
- Every hook folder present
- Each hook `README.md`, `hooks.json`, and any bundled scripts
- Note if the folder is absent entirely

Also read:
- `README.md` — to understand the current project state and verified commands
- `contracts/chatbot-api.openapi.yml` — to understand the API contract scope
- `docs/plans/` — list existing plans so the new plan does not duplicate them

---

## Step 2 — Analyse across eight categories

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
- Is there a project-aware `juanjo` reviewer? An `isabel` contract agent? An infra agent?

### Category 5 — Prompt coverage gaps
- What recurring developer tasks have no guided prompt?
- Are existing prompts complete (do they have verification steps, output format, commit instructions)?
- Are there prompts referenced in agent files that do not exist yet?

### Category 6 — Skills coverage

A **skill** is appropriate for a reusable, self-contained capability that is **invoked on demand** to perform a specific action. It differs from:
- An **instruction** — passive, always-on, shapes how AI writes code for matching files
- A **prompt** — a guided multi-step workflow with verification and output format
- An **agent** — a stateful autonomous executor with a defined identity

**The key test for converting an instruction to a skill:**  
> Does this file contain *passive coding conventions* (→ keep as instruction), or does it contain an *action/workflow/checklist to execute* (→ candidate for skill)?

Apply this test to every instruction file:

#### `dev-setup.instructions.md`
- Current `applyTo: README.md, .github/**` — fires when editing README or .github files
- Content is a **setup checklist**: verify tool versions, run setup commands, troubleshoot Node/npm
- This is not a coding convention — it's an on-demand action: *"check my environment is set up correctly"*
- **Verdict**: Strong skill candidate → `setup-local-env` or `check-prerequisites`
- Why: An instruction that injects "install nvm, run dotnet --version" into every README edit is noise. A skill you explicitly invoke when onboarding or troubleshooting is much more useful.

#### `testing.instructions.md`
- Current `applyTo: {backend,frontend}/**/*.{cs,ts,tsx}` — fires for all code files
- Content is **mixed**: passive testing conventions (frameworks, patterns, naming) + an active "Verification Before Completion" block listing commands to run
- **Verdict**: Split — keep passive conventions as instruction; extract "Verification Before Completion" into a `verify-build` skill
- Why: "Run `dotnet build`, `npm run build`, `npm run lint` and confirm they pass" is an action, not a coding convention. It belongs in a skill you invoke at the end of a task, not injected into context while writing every test file.

#### `planning.instructions.md`
- Current `applyTo: docs/plans/**/*.md` — fires when editing plan files
- Content is **mixed**: passive format rules (required sections, naming, status markers) + a full plan **template** to scaffold from
- **Verdict**: Partial skill candidate — keep the format rules as instruction; the template scaffold is a `scaffold-plan` skill
- Why: The naming and section rules are genuine conventions that should guide editing any plan file. But the template is a one-time creation action — it belongs in a skill (or a prompt) you invoke when starting a new plan.

#### Instructions that are correctly instructions (do not convert)
- `api-contracts.instructions.md` — pure passive conventions for editing OpenAPI YAML. Auto-loading is correct.
- `backend.instructions.md` — passive coding conventions for backend C# files. Auto-loading is correct.
- `frontend.instructions.md` — passive conventions for frontend TS/TSX. Auto-loading is correct.
- `orleans.instructions.md` — passive grain design rules for C# files. Auto-loading is correct.
- `infrastructure.instructions.md` — passive conventions for infra files. Auto-loading is correct.

#### Missing skills to consider

Based on the codebase and tech stack, check whether any of the existing skills are stale or whether new gaps have emerged since the last review. Current skills are in `.github/skills/`.

### Category 7 — Hook coverage

- Are any reusable safety hooks missing for this repo's workflow?
- Do existing hooks have clear installation/configuration guidance?
- Do hook scripts avoid printing secrets or other sensitive values back to the terminal?
- Are hook files discoverable from `README.md` or other Copilot workflow docs?

### Category 8 — Staleness and accuracy
- Do any files reference paths, commands, plans, or features that no longer exist?
- Are verified commands still accurate against the real `package.json`, `.sln`, and `vite.config.*`?
- Do instruction files still match the actual project structure and tech stack?

---

## Step 3 — Score and prioritise findings

For each finding, assign:
- **Impact**: High / Medium / Low
- **Effort**: Small (< 30 min) / Medium (30–90 min) / Large (> 90 min)
- **Category**: one of the eight above

Group findings into:
- **Must fix** — High impact regardless of effort
- **Should fix** — Medium impact, Small or Medium effort
- **Nice to have** — Low impact or Large effort

---

## Step 4 — Write the plan document

Create the file at:
```
docs/plans/copilot-config-improvement-<YYYY-MM-DD>-plan.md
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
- [`docs/plans/copilot-config-improvement-<YYYY-MM-DD>-plan.md`](docs/plans/copilot-config-improvement-<YYYY-MM-DD>-plan.md) — Copilot setup review: prioritized .github/ improvements.
```

---

## Step 6 — Stop

**Do not implement any change from the plan.**

The plan is for human review and approval. It will be executed in a future session when approved.

---

## Output

Provide a concise summary:
- **Findings count by category**: e.g. "Redundancy: 2, applyTo: 3, Coverage gaps: 1, Skills: 2, Hooks: 1, Staleness: 1"
- **Plan created at**: file path
- **Top 3 must-fix items**: brief list
- **Next step**: "Review `docs/plans/copilot-config-improvement-<date>-plan.md` and run this plan when ready"
