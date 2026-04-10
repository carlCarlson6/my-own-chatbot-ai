---
description: "Audit every file under docs/plans/ and update their status markers to match the actual repo state. Does NOT modify implementation files — only updates plan documents and README status entries."
name: "Review Plans"
argument-hint: "Optional: a specific plan file to audit, e.g. 'frontend-chat-ui-plan.md', or leave blank to audit all plans"
agent: "agent"
---

Audit all plan documents under `docs/plans/` and synchronise their status markers with the actual state of the repository. **Do not modify any implementation files.** The only files you may change are files under `docs/plans/` and `README.md`.

---

## Step 1 — Inventory all plan files

1. List every file under `docs/plans/`, including `docs/plans/old/` if it exists.
2. Note the full relative path of each file — you will process them all.
3. If an `argument-hint` value was provided and matches a specific file name, still audit all plans but prioritise that file first.

---

## Step 2 — For each plan file, read and verify

For every plan file found in Step 1, perform the following:

### 2a — Read the plan fully

Read the entire plan document before doing any repo checks. Note:
- The overall goal of the plan
- Each phase, its status marker (`⏳ Pending` or `✅ Done`), and its checklist items
- The stated acceptance criteria
- The `_Last updated:_` date

### 2b — Check the actual repo state

For each **phase** in the plan:

1. **Identify what code the phase describes** — specific files, folders, endpoints, components, grain methods, config entries, etc.
2. **Check whether that code actually exists** in the repo using file reads, `grep`, or directory listings.
3. **Evaluate the acceptance criteria** listed for the phase (or the plan's global acceptance criteria) against the real codebase:
   - Does the file/folder exist at the expected path?
   - Do the key symbols (classes, functions, components, endpoints) exist?
   - For backend phases: does `dotnet build backend/src/MyOwnChatbotAi.sln` pass? (only run the build once per audit session, not once per phase)
   - For frontend phases: does `cd frontend && npm run build && npm run lint` pass? (same — run once per session)
4. **Determine the correct status**:
   - A phase is `✅ Done` only when there is **concrete evidence** (files exist, symbols found, build passes).
   - A phase is `⏳ Pending` when the described code is absent, incomplete, or was reverted.
   - When in doubt, **leave the marker unchanged** and note it in the summary (Step 5).

### 2c — Rules for status changes

| Current marker | Condition | Action |
|---|---|---|
| `⏳ Pending` | Code exists and criteria pass | Change to `✅ Done` |
| `⏳ Pending` | Code does not exist or criteria fail | Leave as `⏳ Pending` |
| `✅ Done` | Code still exists and criteria still pass | Leave as `✅ Done` |
| `✅ Done` | Code was removed or reverted | Change to `⏳ Pending` |

**Never change a status marker based on assumptions.** Only change it when you have verified evidence.

---

## Step 3 — Update plan files

For each plan where one or more phase statuses changed:

1. Update every changed `⏳ Pending` or `✅ Done` marker in the file.
2. Update `_Last updated:_` to today's date (ISO format `YYYY-MM-DD`).
3. Do **not** rewrite or reformat any other content — only change the status markers and the date.
4. Do **not** create new plan files.
5. Do **not** modify any file outside `docs/plans/` except `README.md` (covered in Step 4).

---

## Step 4 — Update README.md

Open `README.md` and locate the `## Plans` section. Apply the following rules:

- A plan is **fully done** when every phase in the file is `✅ Done`.
- A plan is **re-opened** when it was previously listed under `### Completed` but now has at least one `⏳ Pending` phase.

| Condition | Action |
|---|---|
| Plan newly fully done | Move its entry from `### WIP / In-Progress` → `### Completed` |
| Plan re-opened (has new ⏳ phase) | Move its entry from `### Completed` → `### WIP / In-Progress` |
| No change in plan completeness | Leave README entry as-is |

Do **not** add or remove plan entries — only move them between sections.

---

## Step 5 — Produce a summary report

After all files are updated, output a structured summary directly in your response:

### Plans updated

List each plan file where at least one marker was changed:
```
docs/plans/<file>.md
  - Phase N "<phase title>": ⏳ Pending → ✅ Done  (reason: <one-line evidence>)
  - Phase M "<phase title>": ✅ Done → ⏳ Pending  (reason: <one-line evidence>)
  - _Last updated_ changed to YYYY-MM-DD
```

### Plans already accurate

List each plan file where no markers were changed:
```
docs/plans/<file>.md — no changes needed
```

### Phases that could not be verified

List any phase where the criteria were ambiguous or the code could not be definitively checked:
```
docs/plans/<file>.md — Phase N "<phase title>": marker left unchanged — <reason why verification was inconclusive>
```

### README.md changes

List any entries moved between sections, or "No README changes needed."

---

## Step 6 — Commit and push

Stage all changed files (`docs/plans/**/*.md` and `README.md` only) and commit with exactly this message:

```
Update plan status markers to match current repo state

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

Then push with `git push`.

If no files were changed, skip the commit and note "Nothing to commit — all plans were already accurate."

---

## Hard constraints

- **Do NOT modify** any file with extension `.cs`, `.ts`, `.tsx`, `.json`, `.yml` (except files under `docs/plans/`), or any file outside `docs/plans/` and `README.md`.
- **Do NOT create** new plan files.
- **Do NOT delete** plan files.
- **Do NOT reformat** plan content — only change status markers and the `_Last updated:_` date.
- **Only change a status marker when there is concrete evidence.** When in doubt, leave it unchanged and flag it in the summary.
