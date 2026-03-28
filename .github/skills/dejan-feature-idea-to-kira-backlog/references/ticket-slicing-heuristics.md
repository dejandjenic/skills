# Ticket Slicing Heuristics

Use these rules to avoid oversized or unclear tickets.

1. Separate migrations from business logic when rollback risk is high.
2. Separate API contract changes from UI integration when teams differ.
3. Keep each ticket completable within a short iteration.
4. Put cross-cutting observability into its own ticket if broad.
5. Add explicit dependency links whenever a ticket cannot start independently.
