---
name: dejan-prepare-code-review
description: "Use when preparing a local environment for code review by checking out the feature branch for a Kira ticket. Agent fetches remote, checks out the matching branch, and confirms readiness. Trigger phrases: prepare code review, checkout review branch, get branch for ticket, prepare branch."
argument-hint: "Provide projectSlug and ticketId, for example DIT-31."
user-invocable: false
disable-model-invocation: false
---

# Prepare Code Review

Checkout the feature branch for a Kira ticket so the repository is ready for code review.

## Use When
- The user wants to review code for a specific ticket and needs the correct branch checked out locally.
- The user provides a ticket ID and wants the matching remote branch fetched and checked out.

## Do Not Use
- When the user wants to perform the actual code review (use dejan-code-review instead).
- When no ticket ID is provided.
- When the user wants to implement changes (use dejan-workflow-coding-assistant instead).

## Inputs
- Ticket ID, for example `DIT-31` (required)

## Workflow
1. Parse the ticket ID from the user input. The ticket ID is the full key including the project prefix, for example `DIT-31`.
2. Fetch the latest branches from the remote repository using `git fetch origin`.
3. Determine the branch name to check out:
   - Try the ticket ID directly as the branch name, for example `DIT-31`.
   - If not found, try common prefixes: `feature/<ticketId>`, `bugfix/<ticketId>`, `fix/<ticketId>`.
   - If multiple matches exist, prefer the one that starts with the ticket ID.
4. Check out the branch:
   - If the branch exists on the remote, check it out locally tracking the remote branch.
   - If the branch already exists locally, switch to it and pull latest changes.
5. Confirm to the user which branch was checked out and that the repository is ready for code review.

## Output Format
- Ticket ID provided
- Remote fetch result
- Branch name checked out
- Confirmation that the repository is ready for code review

## Critical Rules
1. Always fetch from remote before attempting to check out. Never rely on stale local branch state.
2. If the branch cannot be found on the remote after trying all naming patterns, stop and report the failure. Ask the user to provide the exact branch name.
3. Do not perform any code review analysis. This skill only prepares the branch.
4. Do not modify any files. Only perform git operations.
5. If the checkout fails due to uncommitted local changes, inform the user and suggest stashing or committing before retrying.
