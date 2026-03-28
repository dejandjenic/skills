---
name: "dejan.workflow-coding-assistant"
description: "Implement features, fix bugs, add tests, or refactor code in this repository with minimal safe diffs and validation."
argument-hint: "Describe the coding task and constraints"
agent: "agent"
---

Use the workflow in [Workflow Coding Assistant](../skills/dejan-workflow-coding-assistant/SKILL.md) as the source of truth.

Execute the coding task end to end:
- gather repository context before editing
- make the smallest safe change that satisfies the request
- add or update tests when behavior changes
- run available validation and report outcomes
- summarize what changed, why it changed, validation performed, risks, and follow-up options

Preserve existing style and avoid unrelated edits.