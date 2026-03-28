---
name: "dejan.feature-idea-to-kira-backlog"
description: "Turn a rough feature idea into a validated implementation plan and create or update multiple Kira tickets through MCP."
argument-hint: "Provide feature idea, desired outcome, constraints, target projectSlug, and mode create-only/update-only/mixed"
agent: "agent"
---

Use the workflow in [Feature Idea To Kira Backlog](../skills/dejan-feature-idea-to-kira-backlog/SKILL.md) as the source of truth.

Execute the workflow end to end:
- ask only the discovery questions needed to unblock the task
- continue with explicit assumptions and confidence labels when inputs are missing
- inspect the relevant codebase context before proposing tickets
- produce a concise implementation spec and decompose the work into actionable tickets
- if Kira write operations are requested, validate the project slug before any create or update
- return created or updated ticket IDs, field diffs, assumptions, open questions, and the next action

Prefer the templates and references linked from the skill file.