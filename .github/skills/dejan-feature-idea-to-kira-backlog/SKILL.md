---
name: dejan-feature-idea-to-kira-backlog
description: "Use when turning a rough feature idea into a validated implementation plan and then creating or updating multiple Kira tickets through MCP. Includes discovery questions, codebase inspection, architecture analysis, ticket decomposition, and backlog sync. Trigger phrases: feature to tickets, analyze idea and create tickets, backlog generation, Kira planning, implementation backlog."
argument-hint: "Provide feature idea, desired outcome, constraints, target projectSlug, and mode create-only/update-only/mixed"
user-invocable: false
disable-model-invocation: false
---

# Feature Idea To Kira Backlog

End-to-end workflow that converts an incomplete feature concept into implementation-ready backlog tickets in Kira.

## Use When
- A feature idea exists, but details are incomplete.
- You want structured discovery before committing to implementation.
- You need multiple actionable tickets created or updated in Kira.

## Do Not Use
- Tiny bug fixes that need a single direct ticket.
- Situations where Kira MCP is unavailable.

## Inputs
- Feature idea and business goal
- Known constraints and deadlines
- Kira project slug, for example `my-project`
- Original ticket ID, for example `PROJ-123` (required when this run is tied to an existing ticket)
- Mode: create-only, update-only, or mixed
- Optional existing ticket IDs to update
- Preferences for key architectural choices when relevant

## Phase Workflow
1. Original Ticket Start Transition
- If an original ticket ID is provided, read the full ticket content using `get_ticket(projectSlug, ticketId)` via Kira MCP to gather context and validate existence.
- Immediately update the original ticket status to `InProgress` at workflow start.
- If this transition fails, stop and report the failure before continuing.

2. Discovery Questions
- Ask targeted questions from [discovery questions](./assets/discovery-questions.md).
- If user cannot answer, continue with assumptions and confidence labels.
- Do not lock into the first implementation path before exploring options.

3. Option Exploration And Decision Gate
- Before architecture synthesis, identify high-impact decisions (for example auth model, data flow, storage, API style, rollout strategy).
- Present at least two viable options for each high-impact decision with concise trade-offs.
- Ask the user to choose the preferred option before finalizing architecture and ticket plan.
- If the user does not choose, proceed with explicit assumptions and confidence labels and highlight a validation task.
- Guide the user with recommendations, not just implementation defaults.

4. Codebase Inspection
- Inspect relevant modules, APIs, data models, and integration points.
- Capture architectural impact and unknowns in a concise map.

5. Solution Spec Synthesis
- Produce an implementation spec using [spec summary template](./assets/spec-summary-template.md).
- Define scope, non-goals, acceptance criteria, risks, and rollout notes.

6. Ticket Decomposition
- Break work into multiple tickets with clear boundaries:
  - foundation and migration
  - backend and API
  - frontend or client
  - testing and observability
  - rollout and documentation
- Use [ticket decomposition template](./assets/ticket-decomposition-template.md).

7. Kira Backlog Sync (MCP)
- Validate slug before write operations with `get_project` (fallback `list_projects`).
- For each proposed ticket:
  - create with `create_ticket(projectSlug, title, description, priority)`, or
  - update with `update_ticket(projectSlug, ticketId, title, description, status, priority, tags)` when mapped.
- After creating or updating each decomposed ticket:
  - Add the tag `ImplementationReady` to the ticket.
  - If an original ticket ID is provided, add a `depends on` relation from the decomposed ticket to the original ticket using the appropriate Kira MCP relation operation.
- Never invent keys or slugs.
- Use only supported enums:
  - status: `Backlog`, `ToDo`, `InProgress`, `CodeReview`, `Done`
  - priority: `Critical`, `High`, `Medium`, `Low`
- Return created or updated IDs and field diffs.

8. Final Handoff
- If an original ticket ID is provided and the workflow completed successfully, update the original ticket status to `Done`.
- If completion transition to `Done` fails, report failure explicitly.
- Return a complete summary using [run report template](./assets/run-report-template.md).

## Critical Rules
1. Never block on unknowns. Track assumptions explicitly.
2. Do not perform Kira write operations until slug is validated.
3. If a requested update ticket does not exist, stop that item and report it.
4. Ensure each ticket has measurable acceptance criteria.
5. Keep ticket descriptions implementation-oriented, not vague goals.
6. Do not attempt unsupported ticket fields such as assignee or due date.
7. For runs tied to an original ticket, status transitions are mandatory: `InProgress` at start and `Done` at completion.
8. For high-impact architecture decisions, always explore options and ask for user choice before finalizing architecture and tickets.
9. Do not silently implement the first discovered path when meaningful alternatives exist.
10. Never propose storing long-lived API keys or secrets in browser local storage as a default pattern.
11. For any MCP write operation (create, update), retry up to 3 times on failure. After 3 failed attempts, inform the user with the exact MCP error and ask: "Continue anyway or stop here?" Do not proceed without explicit user decision.
12. If the user chooses to continue after MCP failures, report the final failure explicitly and stop the feature workflow.

## Required Output
1. Discovery answers and assumptions
2. Decision log for high-impact choices: options considered, trade-offs, user selections, and unresolved assumptions
3. Architecture impact summary
4. Proposed ticket set
5. Kira execution results: created and updated ticket IDs, with `ImplementationReady` tag and `depends on` relation applied to each decomposed ticket
6. Original ticket transition log: start transition (`InProgress`) and completion transition (`Done`)
7. Remaining open questions and next action

## Optional References
- [ticket slicing heuristics](./references/ticket-slicing-heuristics.md)
- [kira operation checklist](./references/kira-operation-checklist.md)
