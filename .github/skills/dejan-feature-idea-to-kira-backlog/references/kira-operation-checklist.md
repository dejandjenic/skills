# Kira Operation Checklist

Before write operations:
- Confirm Kira MCP server is available.
- Validate project slug exists.
- Confirm mode and mapping rules.

During write operations:
- Create or update one ticket at a time.
- Capture key and changed fields for every operation.
- Stop and report item-level failures without losing successful items.

After write operations:
- Return all created or updated keys.
- Return failures with reason and retry suggestion.
