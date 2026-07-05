---
name: agent-code-review
description: "Autonomous code review agent for orchestrator use. Reviews code and signals outcome without user confirmation. Trigger phrase: agent review."
argument-hint: "Provide projectSlug and ticketId for autonomous code review."
user-invocable: false
disable-model-invocation: false
---

# Agent Code Review

Autonomous code review agent variant designed for orchestrator use. Executes the full review workflow without user interaction and signals the outcome (passed or failed) to the orchestrator.

## Use When
- Orchestrator triggers autonomous code review
- Ticket needs to be reviewed without user intervention
- Automated review workflow execution is required

## Do Not Use
- When user interaction is required
- For exploratory review discussions
- When manual approval gates are needed

## Inputs
- Project slug, for example `my-project`
- Ticket ID, for example `PROJ-123` (required)

## Workflow

**Before anything else:** call `signal_agent_started(ticketId)` on the **dirigent** MCP server. This is mandatory and must be your very first action — it lets the orchestrator detect a stuck/wedged session quickly instead of waiting for the full timeout.

1. Read the full ticket content using `get_ticket(projectSlug, ticketId)` via Kira MCP to obtain acceptance criteria and requirements.
2. If the ticket is not already in `CodeReview` status, move it to `CodeReview` immediately.
3. Examine the current git branch:
   - Identify the branch name (should match the ticket ID pattern).
   - Report the branch name being reviewed.
   - List all commits on this branch not yet on main using `git log main..HEAD --oneline`.
   - Show the full diff using `git diff main`.
4. Find the pull request for the current branch using GitHub MCP tools.
5. Analyze the implementation against ticket acceptance criteria:
   - Verify each acceptance criterion is addressed by the code changes.
   - Check for incomplete work or missing functionality.
   - Identify any scope creep or unrelated changes.
   - Assess code quality: structure, tests, error handling, performance.
6. Document all findings in a review report:
   - Criteria met vs not met
   - Potential issues and risks
   - Quality observations
7. Post the review report as a comment on the Kira ticket using `kira_create_comment(projectSlug, ticketId, body)` with the full review results formatted in markdown.
8. If there are any unmet criteria, missing tests, or quality concerns (review FAILED):
   - Post the review findings as a comment on the GitHub PR using GitHub MCP tools.
   - Keep ticket in `CodeReview` status.
   - Call `signal_review_result(ticketId, false, reason)` on the **dirigent** MCP server, where `reason` is a brief summary of the blocking issues. This is mandatory.
9. If all criteria are met, no blockers are identified, and quality is acceptable (review PASSED):
   - Approve the pull request using GitHub MCP tools.
   - Merge the pull request using GitHub MCP tools.
   - Move ticket to `Done` status.
   - Call `signal_review_result(ticketId, true)` on the **dirigent** MCP server. This is mandatory.
10. Return the review outcome: passed or failed, with ticket ID, PR URL, and brief reason if failed.

## Output Format
- Ticket title and acceptance criteria (from Kira read)
- Branch being reviewed (explicitly state the branch name)
- GitHub PR found (with PR URL)
- Git commits summary
- Code diff analysis
- Acceptance criteria validation: met vs not met
- Quality findings: issues, risks, and observations
- Kira comment posted (confirmation)
- PR action taken: approved and merged, or commented with findings
- Ticket transition result
- Review outcome signal: PASSED (ticket → Done) or FAILED (ticket stays in CodeReview with reason)

## Critical Rules
1. Never ask for user confirmation at any step. Execute all operations autonomously.
2. Always read ticket content first via `get_ticket(projectSlug, ticketId)` on Kira MCP. Never assume acceptance criteria.
3. Always report the branch name being reviewed. Never proceed without identifying the current branch.
4. Always find the pull request for the branch using GitHub MCP tools. Never proceed without identifying the PR.
5. Code review is based solely on acceptance criteria from Kira, not personal opinion.
6. Always examine the actual git diff. Never infer changes from discussion or commit messages.
7. Always post the full review report as a comment on the Kira ticket before making any status transition or PR action.
8. If the review passes: approve and merge the PR, move ticket to `Done`, then call `signal_review_result(ticketId, true)` on the dirigent MCP server.
9. If the review fails: post findings as a PR comment, keep ticket in `CodeReview`, then call `signal_review_result(ticketId, false, reason)` on the dirigent MCP server.
10. Move ticket to `Done` only if all acceptance criteria are met AND no blockers are identified AND the PR has been successfully merged.
11. If any step fails, retry up to 3 times. After 3 failures, stop and report the error.
12. Call `signal_agent_started(ticketId)` on the dirigent MCP server as your very first action, before any other step. This is mandatory and separate from `signal_review_result`.

## Quality Bar
- Complete acceptance criteria validation against actual code changes.
- Clear separation of facts (criteria met/not met) from recommendations.
- Explicit list of any unmet criteria or blockers.
- PR is approved and merged only when review passes completely.
- PR receives detailed comment with findings when review fails.
- All operations execute without user interaction.
