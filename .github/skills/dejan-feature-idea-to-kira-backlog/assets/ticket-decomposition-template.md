# Ticket Decomposition Template

## Decomposition Principles
- One ticket should produce one meaningful increment.
- Dependencies must be explicit.
- Acceptance criteria must be testable.

## Ticket Table
| Temp ID | Type | Summary | Description | Depends On | Acceptance Criteria | Target Owner |
|---|---|---|---|---|---|---|
| T1 | Foundation |  |  |  |  |  |
| T2 | Backend/API |  |  | T1 |  |  |
| T3 | Frontend/Client |  |  | T2 |  |  |
| T4 | Testing/Observability |  |  | T2,T3 |  |  |
| T5 | Rollout/Docs |  |  | T4 |  |  |

## Mapping Rules
- If mode is create-only: all Temp IDs create new Kira tickets.
- If mode is update-only: each Temp ID must map to an existing Kira key.
- If mode is mixed: use explicit mapping table below.

## Existing Ticket Mapping (Mixed Mode)
| Temp ID | Action | Existing Key (if update) |
|---|---|---|
| T1 | create/update |  |
