#!/bin/bash
# graphify-hook-start
# Auto-rebuilds OpenWiki docs and Graphify knowledge graph after each commit.
# Installed by: dejan-skills tools

echo "--------------------------------------------------"
echo "🚀 Running post-commit hooks..."
echo "--------------------------------------------------"

# ==================================================
# Step 1: Run OpenWiki via LiteLLM Proxy
# ==================================================
echo "📚 Step 1: Updating OpenWiki documentation via LiteLLM..."



# Run OpenWiki update in code mode
if command -v openwiki >/dev/null 2>&1; then
    openwiki code --update --print || echo "[openwiki hook] Update skipped or failed."
else
    echo "[openwiki hook] openwiki command not found, skipping."
fi

# ==================================================
# Step 2: Run Graphify Knowledge Graph Rebuild
# ==================================================
echo "🕸️ Step 2: Rebuilding Graphify knowledge graph..."

CHANGED=$(git diff --name-only HEAD~1 HEAD 2>/dev/null || git diff --name-only HEAD 2>/dev/null)
if [ -z "$CHANGED" ]; then
    exit 0
fi

export GRAPHIFY_CHANGED="$CHANGED"
python -c "
import os, sys
from pathlib import Path

CODE_EXTS = {
    '.py', '.ts', '.js', '.go', '.rs', '.java', '.cpp', '.c', '.rb', '.swift',
    '.kt', '.cs', '.scala', '.php', '.cc', '.cxx', '.hpp', '.h', '.kts', '.md'
}

changed_raw = os.environ.get('GRAPHIFY_CHANGED', '')
changed = [Path(f.strip()) for f in changed_raw.strip().splitlines() if f.strip()]
code_changed = [f for f in changed if f.suffix.lower() in CODE_EXTS and f.exists()]

if not code_changed:
    sys.exit(0)

print(f'[graphify hook] {len(code_changed)} file(s) changed - rebuilding graph...')

try:
    from graphify.watch import _rebuild_code
    _rebuild_code(Path('.'))
except Exception as exc:
    print(f'[graphify hook] Rebuild failed: {exc}')
    sys.exit(1)
"
# graphify-hook-end

echo "--------------------------------------------------"
echo "✅ Post-commit pipeline complete!"
echo "--------------------------------------------------"
