---
type: Architecture
title: Architecture Overview
description: Overview of repository structure, multi-ecosystem platform layouts (.github, .claude, .opencode), and asset distribution.
tags: [architecture, structure, platforms, sync]
---

# Architecture Overview

The `skills` repository is structured around a central distribution model: master definitions of prompts and skills live in the repository root and are synchronized to various AI assistant platform layouts across target repositories using the `dejan-skills` CLI tool.

## Directory Layout

```mermaid
graph TD
    Root[/] --> Github[.github/]
    Root --> Claude[.claude/]
    Root --> OpenCode[.opencode/]
    Root --> Src[src/Dejan.Skills.Tool/]
    
    Github --> GSkills[skills/]
    Github --> GPrompts[prompts/]
    Github --> GTools[tools/]
    
    Claude --> CSkills[skills/]
    OpenCode --> OSkills[skills/]
    
    Src --> Engine[SyncEngine & ToolsEngine]
```

## Supported Platform Layouts

The synchronization engine (`SyncEngine`) supports three major AI assistant platform layouts simultaneously:

1. **GitHub Copilot (`.github`)**:
   - Skills stored in `.github/skills/`
   - Prompt aliases stored in `.github/prompts/`
2. **Claude (`.claude`)**:
   - Skills synchronized into `.claude/skills/`
3. **OpenCode (`.opencode`)**:
   - Skills synchronized into `.opencode/skills/`

## Related Components

- For details on individual skills, see the [Skills Catalog](/openwiki/skills/catalog.md).
- To learn how to run synchronization commands, consult the [Dejan Skills Tool Guide](/openwiki/tools/dejan-skills.md).
- For automated publishing pipelines, see [Workflows & CI/CD](/openwiki/workflows/sync-and-ci.md).
