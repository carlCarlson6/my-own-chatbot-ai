---
description: "Execute the next owned task block from a Danny-authored plan. Stops on ownership mismatch so orchestration can hand off to the correct agent."
name: "Start Plan Task"
argument-hint: "Required: relative plan path, e.g. 'docs/plans/clerk-auth-multi-conversation-plan.md'"
agent: "agent"
---

Start executing work from a plan created by `danny`.

This prompt is for **single-agent worker execution**. It is the right prompt when the active agent should either execute its own next task block or stop with a clean handoff.

If the user wants **cross-agent orchestration from one entry point**, use `execute-plan-orchestrated.prompt.md` with the `danny` agent instead of this prompt.

The input argument is required and must be the relative path to the plan file under `docs/plans/`.

Use these references throughout:
- [Workspace instructions](../copilot-instructions.md)
- [README](../../README.md)
- [Plan review prompt](./review-plans.prompt.md)
- [Contract instructions](../instructions/api-contracts.instructions.md)
- [Backend instructions](../instructions/backend.instructions.md)
- [Orleans instructions](../instructions/orleans.instructions.md)
- [Frontend instructions](../instructions/frontend.instructions.md)
- [Infrastructure instructions](../instructions/infrastructure.instructions.md)
- [Planning instructions](../instructions/planning.instructions.md)
- [Testing instructions](../instructions/testing.instructions.md)

---

## Step 1 — Read the plan and current repo state

Before doing any implementation work:

1. Read the plan file provided in the prompt argument fully.
2. Read `README.md`.
3. If the plan mentions `contracts/chatbot-api.openapi.yml`, read it.
4. Execute the plan's `## Pre-Implementation Change Review` gate before writing code.
5. Read the implementation folders and documentation relevant to the current pending phase.

---

## Step 2 — Review recent work before choosing the next task

Review what has already been done so you do not duplicate or skip work:

1. Review the recent repo history:
   - `git --no-pager log --oneline -10`
2. Review recent commits touching the plan:
   - `git --no-pager log --oneline -- <plan-path>`
3. Review recent changes touching the files or folders related to the current pending phase.
4. Compare the latest repo state with the plan and update the plan first if it is stale before implementing anything else.

---

## Step 3 — Identify the next actionable task

You must follow the plan order strictly.

1. Find the **first phase** marked `⏳ Pending`.
2. Inside that phase, identify the **first actionable task block** in order.
   - In Danny-authored plans this is usually grouped under an agent label such as `- \`salva\``, `- \`aitor\``, `- \`ivan\``, or `- \`vicente\``.
3. Determine which agent owns that first task block.
4. Determine **your current agent identity** from your active system/agent instructions.

---

## Step 4 — Ownership gate

If the next task block belongs to a different agent:

1. **Stop immediately.**
2. Do **not** modify any files.
3. Do **not** commit.
4. Respond with a short handoff that includes:
   - `Plan`: `<plan-path>`
   - `Current pending phase`: `<phase heading>`
   - `Next task owner`: `<agent-name>`
   - `Next task`: `<copied task block or first task line>`
   - `Reason`: why the user should switch agents before continuing

If the next task block belongs to **you**, continue to Step 5.

If ownership is ambiguous, stop and ask the user to clarify before making changes.

If the task belongs to **you**, that means you may work only inside **your assigned domain** for that specific next task block. You are **not allowed** to implement work that belongs to another agent's domain, even if it feels closely related or blocking.

---

## Step 5 — Execute only the owned task block

When the next task block belongs to you:

1. Read the code, docs, and configuration directly related to that task.
2. Implement only:
   - the selected task block,
   - changes that stay strictly inside your domain boundary,
   - any required plan-status updates documenting what was completed.
3. If completing the task would require changes in another domain, **stop and hand off** instead of making those changes yourself.
4. Do **not** skip ahead to later task blocks from the same phase unless they are part of the same coherent owned task and still remain entirely within your domain.
5. Preserve the plan's dependency order. If you discover the plan is stale or another agent's prerequisite is actually missing, stop and report that instead of improvising across domains.

---

## Step 6 — Validate and update the plan

After implementing the task:

1. Run the relevant verification commands for the files you changed.
2. Update the plan file to reflect what was completed:
   - mark completed items or owned task blocks appropriately,
   - update status markers if the phase status changed,
   - update `_Last updated: YYYY-MM-DD_` when the plan file was changed.
3. If the plan completion changes how it should be listed in `README.md`, update the `## Plans` section as required by the planning instructions.

---

## Step 7 — Commit and push

If you made changes, stage them and commit with:

1. An imperative subject line summarizing the change.
2. A commit body that includes all of the following lines:

```text
Agent: <agent-name>
Plan: <relative plan path>
Phase: <phase heading>
Task: <task block summary>
Summary: <one-line summary of the completed work>

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

Then push with `git push`.

If you made no changes because the task belongs to another agent or the plan is blocked, do not commit.

---

## Output

If you stopped because another agent owns the next task, provide only the handoff summary from Step 4.

If you completed work, provide:
- **Plan**: file path
- **Phase**: current phase
- **Task executed**: the owned task block you completed
- **Files changed**: list
- **Validation**: commands run and results
- **Plan updates**: what status markers or notes changed
- **Commit**: commit SHA and subject

---

## Hard constraints

- **This prompt is for worker agents, not orchestration.**
- **Do not skip the first pending phase.**
- **Do not skip the first task block in that phase.**
- **Work only on the next task block assigned to your agent.**
- **Do not continue when another agent owns the next task.**
- **Do not modify files or implement logic that belongs to another agent's domain.**
- **Do not rewrite the plan structure unless the plan is stale and must be corrected first.**
- **Do not invent agent ownership.** Use the plan text and the active agent identity.
