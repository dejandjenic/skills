---
name: "dejan.kira-mcp-ticket-ops"
description: "Operate on Kira through MCP: list projects, validate slugs, fetch tickets, create tickets, and update supported ticket fields."
argument-hint: "Describe Kira action and inputs: projectSlug, ticketId, title, description, status, priority, tags"
agent: "agent"
---

Use the workflow in [Kira MCP Ticket Ops](../skills/dejan-kira-mcp-ticket-ops/SKILL.md) as the source of truth.

Perform the requested Kira operation carefully:
- never invent project slugs or ticket IDs
- list projects first when the slug is missing
- fetch current ticket state before updates
- apply only minimal supported field changes
- use only supported status and priority values
- return the action performed, inputs used, result, changed fields, and the next recommended action

Prefer the templates and references linked from the skill file.