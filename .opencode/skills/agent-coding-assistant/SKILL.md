---
name: agent-coding-assistant
description: "Autonomous implementation agent for orchestrator use. Executes implementation without user confirmation. Trigger phrase: agent implement."
argument-hint: "Provide projectSlug and ticketId for autonomous implementation."
user-invocable: false
disable-model-invocation: false
---

# Agent Coding Assistant

Autonomous implementation agent variant designed for orchestrator use. Executes the full implementation workflow without user interaction or confirmation.

## Use When
- Orchestrator triggers autonomous implementation
- Ticket needs to be implemented without user intervention
- Automated workflow execution is required

## Do Not Use
- When user interaction is required
- For exploratory or brainstorming tasks
- When manual approval gates are needed

## Inputs
- Project slug, for example `my-project`
- Ticket ID, for example `PROJ-123` (required)

## Workflow

**Step zero, before reading anything else:** call `signal_agent_started(ticketId)` on the **dirigent** MCP server. Do this literally before your first git command or ticket read. A separate process is waiting on this specific tool call to know you're alive — nothing else you do substitutes for it.

1. Checkout the latest main branch: run `git checkout main && git pull origin main`.
2. Read the full ticket content using `get_ticket(projectSlug, ticketId)` via Kira MCP.
   - Derive all acceptance criteria, scope, and constraints from the ticket content.
   - If reading the ticket fails, stop and report the error — do not proceed.
3. Move the ticket status to `InProgress` immediately.
   - Add the agent identity tag to the ticket: `Kilo`, `Claude`, or `OpenCode`.
4. Create a local git branch named after the ticket ID (for example `git checkout -b PROJ-123` or `git checkout -b feature/PROJ-123-short-title`).
   - If a branch for this ticket already exists, check it out instead of creating a new one.
5. Gather context from the repository.
6. Create the smallest safe code change that satisfies the ticket acceptance criteria.
7. Add or update tests when behavior changes.
8. Run available checks. If checks fail, attempt to fix issues autonomously.
9. Stage all changed files and commit immediately without user approval: `git commit -m "<ticket-id>: <description>"`.
10. Push the branch to the remote repository: `git push -u origin <branch-name>`.
11. Create a pull request on GitHub using GitHub MCP tools. The PR title includes the ticket ID (e.g., `PROJ-123: Short description`). The PR body summarizes changes, lists modified files, and references the ticket.
12. Post an implementation comment on the Kira ticket using `kira_create_comment(projectSlug, ticketId, body)` summarizing what was changed, which files were modified, any risks, and the PR URL.
13. Move the ticket status to `CodeReview`.
14. Call `signal_implementation_done(ticketId)` on the **dirigent** MCP server. This is mandatory — it unblocks the orchestrator.
15. Only after the `signal_implementation_done` call in step 14 has actually completed, return summary: changed files, git branch, commit info, PR URL, and ticket transition result.

**This task is not finished when you write your summary — it is finished when `signal_implementation_done` has been called.** A separate orchestrator process is blocked waiting on that specific tool call; nothing else you do (the commit, the PR, the Kira comment, a great summary) reaches it. If you notice you're about to end your turn without having made that call, stop and make it now, before responding. An implementation that isn't signaled is indistinguishable, from the orchestrator's side, from an implementation that never happened at all — no matter how correct or complete your actual work was.

## Output Format
- What changed
- Why it changed
- Validation performed
- Git branch created or checked out
- Commit hash
- Branch pushed to remote
- GitHub PR created (with PR URL)
- Kira implementation comment posted
- Ticket transition: `InProgress` → `CodeReview` (completion signal)

## Critical Rules
1. Never ask for user confirmation at any step. Execute all operations autonomously.
2. Always checkout the latest main branch before any code analysis or edits.
3. Always read ticket content via `get_ticket(projectSlug, ticketId)` on Kira MCP first. Never start coding from assumptions.
4. Do not invent or infer ticket content. If the ticket cannot be read, stop.
5. Ticket status transitions are mandatory: `InProgress` before coding, `CodeReview` after implementation.
6. Always add the agent identity tag when transitioning to `InProgress`.
7. Always create or check out a git branch named after the ticket ID before any file edits.
8. Commit immediately after changes — do not wait for approval.
9. After commit, always push the branch and create a GitHub PR using GitHub MCP tools.
10. After creating the PR, always post a comment on the Kira ticket before moving to `CodeReview`.
11. After moving ticket to `CodeReview`, always call `signal_implementation_done(ticketId)` on the dirigent MCP server — this is the actual signal that unblocks the orchestrator.
12. If any step fails, retry up to 3 times. After 3 failures, stop and report the error.
13. Call `signal_agent_started(ticketId)` on the dirigent MCP server as literally your first tool call, before reading the ticket or running any git command. This is separate from `signal_implementation_done` and equally mandatory.
14. `signal_agent_started` and `signal_implementation_done` are the only two things the orchestrator can actually observe from this session — everything else (the commit, the PR, your summary) is invisible to it. Skipping either call makes an otherwise-perfect implementation indistinguishable from a session that never ran. Do not end your turn without having made both.

## Quality Bar
- Prefer minimal diffs and preserve existing style.
- Avoid unrelated edits.
- Keep behavior and interfaces stable unless explicitly required by acceptance criteria.
- All operations execute without user interaction.
