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
- Mode: create-only, update-only, or mixed
- Optional existing ticket IDs to update

## Phase Workflow
1. Discovery Questions
- Ask targeted questions from [discovery questions](./assets/discovery-questions.md).
- If user cannot answer, continue with assumptions and confidence labels.

2. Codebase Inspection
- Inspect relevant modules, APIs, data models, and integration points.
- Capture architectural impact and unknowns in a concise map.

3. Solution Spec Synthesis
- Produce an implementation spec using [spec summary template](./assets/spec-summary-template.md).
- Define scope, non-goals, acceptance criteria, risks, and rollout notes.

4. Ticket Decomposition
- Break work into multiple tickets with clear boundaries:
  - foundation and migration
  - backend and API
  - frontend or client
  - testing and observability
  - rollout and documentation
- Use [ticket decomposition template](./assets/ticket-decomposition-template.md).

5. Kira Backlog Sync (MCP)
- Validate slug before write operations with `get_project` (fallback `list_projects`).
- For each proposed ticket:
  - create with `create_ticket(projectSlug, title, description, priority)`, or
  - update with `update_ticket(projectSlug, ticketId, title, description, status, priority, tags)` when mapped.
- Never invent keys or slugs.
- Use only supported enums:
  - status: `Backlog`, `ToDo`, `InProgress`, `Done`
  - priority: `Critical`, `High`, `Medium`, `Low`
- Return created or updated IDs and field diffs.

6. Final Handoff
- Return a complete summary using [run report template](./assets/run-report-template.md).

## Critical Rules
1. Never block on unknowns. Track assumptions explicitly.
2. Do not perform Kira write operations until slug is validated.
3. If a requested update ticket does not exist, stop that item and report it.
4. Ensure each ticket has measurable acceptance criteria.
5. Keep ticket descriptions implementation-oriented, not vague goals.
6. Do not attempt unsupported ticket fields such as assignee or due date.

## Required Output
1. Discovery answers and assumptions
2. Architecture impact summary
3. Proposed ticket set
4. Kira execution results: created and updated ticket IDs
5. Remaining open questions and next action

## Optional References
- [ticket slicing heuristics](./references/ticket-slicing-heuristics.md)
- [kira operation checklist](./references/kira-operation-checklist.md)
