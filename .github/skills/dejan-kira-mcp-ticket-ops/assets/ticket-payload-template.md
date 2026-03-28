# Ticket Payload Template

Use this checklist before create or update operations.

## Create Payload
- projectSlug:
- title:
- description:
- priority:

## Update Payload
- ticketId:
- fieldsToChange:
  - title:
  - description:
  - status:
  - priority:
  - tags: []

## Allowed Enum Values
- status: Backlog, ToDo, InProgress, Done
- priority: Critical, High, Medium, Low

## Validation
- projectSlug exists in Kira project list
- ticketId format looks valid, for example PROJ-1
- required fields are non-empty
- no unintended field overwrites
- tag IDs exist in project when tags are provided
