# Workflow Examples

## Example 1: Create Ticket
User intent: Create a bug ticket in EQI for login timeout issue.

Steps:
1. Call `get_project(EQI)` if slug is known, otherwise call `list_projects` first.
2. Build payload with `title`, `description`, and `priority`.
3. Call `create_ticket(projectSlug, title, description, priority)`.
4. Return ticket ID, for example `EQI-23`, and summary of created fields.

## Example 2: Update Ticket Status
User intent: Move `EQI-23` to In Progress and assign to Alex.

Steps:
1. Fetch `EQI-23` current state with `get_ticket(projectSlug, ticketId)`.
2. Explain that assignee is not supported by the current MCP contract.
3. Apply minimal supported update for `status = InProgress`.
4. Optionally apply `priority`, `title`, `description`, or `tags`.
5. Execute `update_ticket(projectSlug, ticketId, ...)`.
4. Return before and after values for changed fields.

## Example 3: Missing Slug
User intent: Create ticket for payment retry bug, no project provided.

Steps:
1. Call `list_projects`.
2. Ask user to choose target slug.
3. Continue with `create_ticket` payload after slug confirmation.
