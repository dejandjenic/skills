---
name: dejan-code-review
description: "Use when reviewing code changes against ticket requirements. Agent reads ticket from Kira MCP, examines current git branch changes, validates implementation, and moves ticket through CodeReview to Done if quality check passes. Trigger phrases: code review, review changes, review implementation, review PR."
argument-hint: "Provide projectSlug and ticketId for code review."
user-invocable: false
disable-model-invocation: false
---

# Code Review

Dedicated workflow for reviewing implementation changes against ticket requirements using Kira MCP as the source of truth.

## Use When
- Code changes have been committed to a feature branch for a ticket.
- You want to validate implementation against ticket acceptance criteria.
- You want automated quality feedback before merging to main.

## Do Not Use
- When Kira MCP is unavailable.
- When no code changes are present on the feature branch.
- For style-only reviews without acceptance criteria validation.

## Inputs
- Project slug, for example `my-project`
- Ticket ID, for example `PROJ-123` (required)

## Workflow
1. Read the full ticket content using `get_ticket(projectSlug, ticketId)` via Kira MCP to obtain acceptance criteria and requirements.
2. If the ticket is not already in `CodeReview` status, move it to `CodeReview` immediately.
3. Examine the current git branch:
   - Identify the branch name (should match the ticket ID pattern).
   - List all uncommitted changes using `git status`.
   - List all commits on this branch not yet on main using `git log main..HEAD --oneline`.
   - Show the full diff using `git diff main` or fetch staged changes using `git diff --staged`.
4. Analyze the implementation against ticket acceptance criteria:
   - Verify each acceptance criterion is addressed by the code changes.
   - Check for incomplete work or missing functionality.
   - Identify any scope creep or unrelated changes.
   - Assess code quality: structure, tests, error handling, performance.
5. Document all findings in a review report:
   - Criteria met vs not met
   - Potential issues and risks
   - Quality observations
6. Post the review report as a comment on the Kira ticket using `kira_create_comment(projectSlug, ticketId, body)` with the full review results formatted in markdown.
7. If there are any unmet criteria, missing tests, or quality concerns, inform the user and keep ticket in `CodeReview` status.
8. If all criteria are met, no blockers are identified, and quality is acceptable, move ticket to `Done` status.
9. Return the review report with ticket transition result.

## Output Format
- Ticket title and acceptance criteria (from Kira read)
- Git branch and commits summary
- Code diff analysis
- Acceptance criteria validation: met vs not met
- Quality findings: issues, risks, and observations
- Recommendation: proceed to Done or hold in CodeReview
- Kira comment posted (confirmation)
- Ticket transition result

## Critical Rules
1. Always read ticket content first via `get_ticket(projectSlug, ticketId)` on Kira MCP. Never assume acceptance criteria.
2. Code review is based solely on acceptance criteria from Kira, not personal opinion.
3. Always examine the actual git diff. Never infer changes from discussion or commit messages.
4. Move ticket to `CodeReview` at the start of the review (step 2).
5. Always post the full review report as a comment on the Kira ticket using `kira_create_comment` before making any status transition (step 6). The comment must be posted regardless of whether the review passes or fails.
6. Move ticket to `Done` only if all acceptance criteria are met AND no blockers are identified.
7. If ticket transition fails, retry up to 3 times. After 3 failed attempts, inform the user with the MCP error details and ask: "Continue anyway or stop here?" Wait for explicit user decision before proceeding.
8. If ticket transition fails after user decides to continue, report the failure explicitly.
9. Always inform the user of potential issues, even if only minor concerns.

## Quality Bar
- Complete acceptance criteria validation against actual code changes.
- Clear separation of facts (criteria met/not met) from recommendations.
- Explicit list of any unmet criteria or blockers.
- No silent approvals — user sees full analysis before ticket moves to Done.
