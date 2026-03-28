---
name: dejan-workflow-coding-assistant
description: "Use when implementing features, fixing bugs, adding tests, or refactoring code in this repository. Trigger phrases: implement, fix, refactor, add tests, scaffold, wire up."
argument-hint: "Describe the coding task and constraints"
user-invocable: false
disable-model-invocation: false
---

# Workflow Coding Assistant

## Use When
- You need to implement a feature end to end
- You need to debug and fix a bug
- You need to add or improve tests
- You need to refactor while preserving behavior

## Do Not Use
- For purely product or UX brainstorming with no code changes
- For one-line factual questions unrelated to code

## Inputs
- User goal and acceptance criteria
- Relevant files, modules, or symbols
- Runtime, framework, and test constraints
- Optional ticket context: project slug and ticket ID when implementation is tied to a tracked ticket

## Workflow
1. If a ticket ID is provided, validate ticket existence and move the ticket status to `InProgress` before code edits.
2. Gather context from the repository before editing.
3. Create the smallest safe code change that satisfies the request.
4. Add or update tests when behavior changes.
5. Run available checks and report outcomes.
6. If implementation is successful and a ticket ID is provided, move the ticket status to `Done`.
7. Summarize changed files, risks, and next steps, including ticket transition results.

## Output Format
- What changed
- Why it changed
- Validation performed
- Ticket transition log: start transition (`InProgress`) and completion transition (`Done`) when ticket context is provided
- Follow-up options

## Quality Bar
- Prefer minimal diffs and preserve existing style.
- Avoid unrelated edits.
- Keep behavior and interfaces stable unless explicitly requested.
- If ticket transitions fail, report failure explicitly with the attempted operation.
