---
description: "Run a Danny-authored plan end-to-end by coordinating the next task block to the correct agent, preserving phase order and updating plan state between handoffs."
name: "Execute Plan Orchestrated"
argument-hint: "Required: relative plan path, e.g. 'docs/plans/clerk-auth-multi-conversation-plan.md'"
agent: "danny"
---

Execute a plan created by `danny` from a single orchestration entry point.

The input argument is required and must be the relative path to the plan file under `docs/plans/`.

Use these references throughout:
- [Workspace instructions](../copilot-instructions.md)
- [README](../../README.md)
- [Plan review prompt](./review-plans.prompt.md)
- [Worker execution prompt](./start-plan-task.prompt.md)
- [Contract instructions](../instructions/api-contracts.instructions.md)
- [Backend instructions](../instructions/backend.instructions.md)
- [Orleans instructions](../instructions/orleans.instructions.md)
- [Frontend instructions](../instructions/frontend.instructions.md)
- [Infrastructure instructions](../instructions/infrastructure.instructions.md)
- [Planning instructions](../instructions/planning.instructions.md)
- [Testing instructions](../instructions/testing.instructions.md)

---

## Purpose

This prompt is for **cross-agent orchestration**, not direct implementation. Your job is to read the plan, inspect current repo state, find the next actionable task block in plan order, launch the correct specialist agent, and keep the plan moving until it is blocked or complete.

Use this prompt when the user wants a **single execution flow** that can move across `contract-updater`, `salva`, `aitor`, `ivan`, `vicente`, and final review steps without manually switching agents between each phase.

Do **not** use this prompt for single-agent implementation work. For that, use `start-plan-task.prompt.md`.

---

## Step 1 — Read the plan and current repo state

Before delegating any work:

1. Read the full plan file provided in the prompt argument.
2. Read `README.md`.
3. If the plan mentions `contracts/chatbot-api.openapi.yml`, read it.
4. Execute the plan's `## Pre-Implementation Change Review` gate before any task delegation.
5. Read the implementation folders and docs relevant to the current pending phase.

---

## Step 2 — Review recent work and detect stale plan state

Before choosing the next task:

1. Review recent repo history:
   - `git --no-pager log --oneline -10`
2. Review recent commits touching the plan:
   - `git --no-pager log --oneline -- <plan-path>`
3. Review recent changes touching the files or folders related to the current pending phase.
4. Compare the latest repo state with the plan and update the plan first if it is stale before delegating implementation.

When the plan is stale:

- Update only the stale plan content needed to restore an accurate execution baseline.
- Do **not** skip ahead to implementation until the plan reflects reality.
- If the stale state creates ambiguity you cannot resolve from the repo, stop and ask the user.

---

## Step 3 — Identify the next actionable task block

You must follow the plan order strictly.

1. Find the **first phase** marked `⏳ Pending`.
2. Inside that phase, identify the **first actionable task block** in order.
3. Determine which agent owns that task block.
4. Confirm whether the selected task depends on unfinished earlier work from another agent in the same or a prior phase.

Rules:

- **Do not skip the first pending phase.**
- **Do not skip the first task block in that phase.**
- **Do not parallelize dependent task blocks.**
- If the first task block is blocked by a missing prerequisite, resolve the prerequisite first or stop and report the blocker.

---

## Step 4 — Delegate to the owning agent

When the next task block belongs to another agent, do **not** stop. Launch the correct specialist agent and give it a complete, domain-specific brief.

Your delegated brief must include:

- **Plan**: relative path
- **Phase**: exact current phase heading
- **Task block**: copy the exact task block text from the plan
- **Objective**: the concrete outcome the agent owns
- **Scope**: which folders/files or domains it may change
- **Non-goals**: which domains/files it must not change
- **Repo reality notes**: anything learned during the pre-implementation review that affects execution
- **Dependencies**: already-completed upstream work and any constraints
- **Expected outputs**: changed files, summary, blockers, follow-ups
- **Acceptance criteria**: the conditions the agent must satisfy
- **Validation**: the commands or checks the agent must run for its work
- **Plan updates required**: what status markers, notes, or dates it must update if it changes the plan

If the next block belongs to `danny`, execute only the orchestration or integration-review work described there. Do not start implementing another agent's domain work yourself unless delegation has clearly failed and the user has implicitly or explicitly accepted that fallback.

---

## Step 5 — Reconcile results after each delegation

After a delegated agent finishes:

1. Read its result carefully.
2. Confirm the work matches the selected task block and did not skip plan order.
3. Check for blockers, assumptions, or follow-up items.
4. Review any changed plan markers or notes.
5. Determine the next actionable task block.

If the delegated work is incomplete, inconsistent with the plan, or missing required validation:

- Send a precise follow-up to the same agent, or
- Route a narrowly scoped follow-up to the correct downstream agent if the first task is genuinely complete.

Do **not** declare the phase complete until the phase's planned work is actually complete.

---

## Step 6 — Continue until blocked or complete

Repeat Steps 3 through 5 until one of the following is true:

1. The plan is complete.
2. A true blocker exists that cannot be resolved from the repo or existing instructions.
3. The user must make a decision on scope, behavior, or architecture.

Use `code-reviewer` for an integration-focused correctness pass when the plan reaches a meaningful cross-domain milestone or final completion.

---

## Step 7 — Output

If blocked, provide:

- **Plan**: file path
- **Current phase**: exact heading
- **Blocked task owner**: agent name
- **Blocked task**: exact task block or first task line
- **Blocker**: what is preventing safe progress
- **Recommended next step**: the minimal user or agent action needed

If complete or partially advanced, provide:

- **Plan**: file path
- **Current phase**: exact heading reached
- **Tasks delegated**: agents and task blocks completed in order
- **Plan updates**: statuses, notes, or dates changed
- **Validation**: what was run and by which agent
- **Open items**: remaining phases, blockers, or "None"

---

## Hard constraints

- **This prompt is only for `danny`.**
- **Do not skip phases or earlier task blocks.**
- **Do not implement another agent's domain work directly when a specialist agent is available.**
- **Do not parallelize dependent task blocks.**
- **Do not leave the plan stale if repo evidence shows it is stale.**
- **Do not mark work complete without an actual delegated result or direct evidence from the repo.**
- **Use `start-plan-task.prompt.md` only as the worker-style reference for single-agent execution, not as the orchestration engine itself.**
