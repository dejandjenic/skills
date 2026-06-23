---
name: dejan-workflow-coding-assistant
description: "Use when implementing features, fixing bugs, adding tests, or refactoring code in this repository. Trigger phrases: implement, fix, refactor, add tests, scaffold, wire up."
argument-hint: "Describe the coding task and constraints. When implementing a ticket, provide projectSlug and ticketId."
user-invocable: false
disable-model-invocation: false
---

# Workflow Coding Assistant

## Use When
- You need to implement a feature end to end
- You need to debug and fix a bug
- You need to add or improve tests
- You need to refactor while preserving behavior
- You need to fix issues identified during code review

## Do Not Use
- For purely product or UX brainstorming with no code changes
- For one-line factual questions unrelated to code

## Inputs
- User goal and acceptance criteria
- Relevant files, modules, or symbols
- Runtime, framework, and test constraints
- Ticket context: project slug and ticket ID (required when the user says "implement ticket" or refers to a ticket)

## Workflow
1. Checkout the latest main branch before any code analysis or edits: run `git checkout main && git pull origin main`. This ensures all subsequent work is based on the most up-to-date codebase.
2. When implementing a ticket:
   a. Read the full ticket content using `get_ticket(projectSlug, ticketId)` via Kira MCP — this is mandatory and must happen before any code is written.
   b. Derive all acceptance criteria, scope, and constraints from the ticket content just read.
   c. Move the ticket status to `InProgress` before code edits.
   d. Add the agent identity tag to the ticket at the same time as the `InProgress` transition. Use the tag that matches the current agent: `Kilo`, `Claude`, or `OpenCode`. As GitHub Copilot (Claude), use the tag `Claude`.
   e. If reading the ticket fails, stop and report the error — do not proceed with implementation.
3. When fixing code review issues:
   a. Read the ticket comments using `kira_list_comments(projectSlug, ticketId)` to identify code review feedback.
   b. Analyze each review comment and determine the required fixes.
   c. Apply the fixes to address all review concerns.
   d. After committing fixes, post a comment using `kira_create_comment(projectSlug, ticketId, body)` explicitly listing which review items were addressed and how.
4. Create a local git branch named after the ticket ID before any file edits (for example `git checkout -b PROJ-123` or `git checkout -b feature/PROJ-123-short-title`). If a branch for this ticket already exists, check it out instead of creating a new one.
5. Gather context from the repository before editing.
6. Create the smallest safe code change that satisfies the ticket acceptance criteria.
7. Add or update tests when behavior changes.
8. Run available checks and report outcomes.
9. If checks pass, ask the user for permission to commit. Proposed commit message must include the ticket ID, for example: `PROJ-123: <short description of change>`. Do not commit without explicit user approval.
10. If the user approves, stage all changed files and run `git commit -m "<ticket-id>: <description>"`.
11. Post a short implementation comment on the Kira ticket using `kira_create_comment(projectSlug, ticketId, body)` summarizing what was changed, which files were modified, and any risks or follow-up notes.
12. If implementation is successful and a ticket ID is provided, move the ticket status to `CodeReview`.
13. Summarize changed files, risks, and next steps, including ticket transition results and git branch/commit info.

## Output Format
- What changed
- Why it changed
- Validation performed
- Git branch created or checked out
- Commit message proposed and user approval result
- Kira implementation comment posted (confirmation)
- Ticket transition log: start transition (`InProgress`) with agent tag applied, and completion transition (`CodeReview`) when ticket context is provided
- Follow-up options

## Critical Rules
1. Always checkout the latest main branch (`git checkout main && git pull origin main`) before any code analysis or edits. Never start work from a stale codebase.
2. "Implement ticket" always means: read the full ticket content via `get_ticket(projectSlug, ticketId)` on Kira MCP first, then implement. Never start coding from memory, conversation context, or assumptions about what the ticket says.
3. Do not invent or infer ticket content. If the ticket cannot be read, stop.
4. Ticket status transitions are mandatory when a ticket ID is provided: `InProgress` before coding, `CodeReview` after successful implementation. When transitioning to `InProgress`, also add the agent identity tag (`Kilo`, `Claude`, or `OpenCode`) to the ticket.
5. Always create or check out a git branch named after the ticket ID before any file edits. Never commit directly to the current branch without branching first.
6. Never commit without explicit user approval. Always show the full proposed commit message and wait for a yes/no before running `git commit`.
7. After a successful commit, always post a short implementation comment on the Kira ticket using `kira_create_comment` before moving the ticket to `CodeReview`. The comment must summarize what was changed and which files were modified.
8. When fixing code review issues, always read the ticket comments first using `kira_list_comments` to understand the review feedback. After applying fixes, post a comment explicitly listing which review items were addressed and how.
9. If a ticket status transition via Kira MCP fails, retry up to 3 times. After 3 failed attempts, inform the user with the MCP error details and ask: "Continue anyway or stop here?" Wait for explicit user decision before proceeding.
10. If ticket transition fails after user decides to continue, report the failure explicitly.

## Quality Bar
- Prefer minimal diffs and preserve existing style.
- Avoid unrelated edits.
- Keep behavior and interfaces stable unless explicitly requested.
- If ticket transitions fail, report failure explicitly with the attempted operation.
