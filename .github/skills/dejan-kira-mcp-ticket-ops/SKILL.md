---
name: dejan-kira-mcp-ticket-ops
description: "Use when working with Kira through MCP: list projects, validate project slugs, list tickets, get tickets, create tickets, and update ticket fields or status. Trigger phrases: Kira ticket, create Kira issue, update Kira issue, Kira project slug, PROJ-1, backlog grooming."
argument-hint: "Describe Kira action and inputs: projectSlug, ticketId, title, description, status, priority, tags"
user-invocable: false
disable-model-invocation: false
---

# Kira MCP Ticket Ops

Use this skill for reliable ticket operations in Kira via MCP.

## Use When
- The user asks to list Kira projects.
- The user asks to create a ticket in a project.
- The user asks to fetch or update ticket details.
- The user provides or references a ticket ID like `PROJ-1`.

## Do Not Use
- When no Kira MCP server is available.
- When the task is pure implementation with no ticket operations.

## Inputs
- Requested action: list, get, create, update
- Project slug when operating on tickets, for example `my-project`
- Ticket ID for read or update, for example `PROJ-1`
- Field updates: title, description, status, priority, tags

## Core Rules
1. Never invent project slugs or ticket IDs.
2. If slug is missing for ticket creation, list projects first and ask user to confirm.
3. If ticket ID is ambiguous or malformed, stop and request correction.
4. Echo intended changes before write operations when the request is ambiguous.
5. After every create or update, return the resulting ticket ID and changed fields.
6. Use only supported values:
   - status: `Backlog`, `ToDo`, `InProgress`, `Done`
   - priority: `Critical`, `High`, `Medium`, `Low`
7. If tags are provided for update, verify IDs with `list_tags` first.

## MCP Workflow
1. Use exact Kira tools:
   - `list_projects`, `get_project`, `create_project`, `delete_project`
   - `list_tickets`, `get_ticket`, `create_ticket`, `update_ticket`, `delete_ticket`
   - `list_tags`, `create_tag`, `delete_tag`
   - `list_attachments`
2. For create requests:
   - validate project slug exists with `get_project` (fallback to `list_projects`)
   - assemble payload using [ticket payload template](./assets/ticket-payload-template.md)
   - execute `create_ticket(projectSlug, title, description, priority)`
   - return created ticket ID and summary
3. For update requests:
   - fetch current ticket first with `get_ticket(projectSlug, ticketId)`
   - apply minimal field changes only
   - execute `update_ticket(projectSlug, ticketId, title, description, status, priority, tags)`
   - return a field diff summary
4. For read requests:
   - return concise state: status, priority, title, tags, updated time if present

## Required Output Format
1. Action performed
2. Inputs used
3. Kira result
4. Next recommended action

## Error Handling
- Missing slug: list projects and request selection.
- Missing required fields for creation: request only missing fields.
- Permission denied: report exact operation and suggest who should run it.
- Not found: confirm ticket ID or slug and offer nearest matches if available.

## Field Mapping Notes
- In Kira tool calls, use `title` instead of summary.
- Ticket updates do not support assignee or due date fields.
- Tag updates use tag IDs via the `tags` field and replace existing tags.

## Optional References
- [workflow examples](./references/workflow-examples.md)
