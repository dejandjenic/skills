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

## Workflow
1. Gather context from the repository before editing.
2. Create the smallest safe code change that satisfies the request.
3. Add or update tests when behavior changes.
4. Run available checks and report outcomes.
5. Summarize changed files, risks, and next steps.

## Output Format
- What changed
- Why it changed
- Validation performed
- Follow-up options

## Quality Bar
- Prefer minimal diffs and preserve existing style.
- Avoid unrelated edits.
- Keep behavior and interfaces stable unless explicitly requested.
