---
type: Operations
title: Operations and Maintainer Guide
description: Maintainer runbooks for tagging releases, publishing the dejan-skills NuGet package, and configuring repository hooks.
resource: file:///README.md
tags: [operations, maintenance, publishing, nuget, git-hooks]
timestamp: 2026-03-31T00:00:00Z
---

# Operations and Maintainer Guide

This guide covers day-to-day maintenance workflows for the `skills` repository and the `Dejan.Skills.Tool` NuGet package.

## Release & Publishing Runbook

To publish a new version of `dejan-skills`:

1. **Tag the Commit**:
   ```bash
   git tag v0.1.1 && git push origin v0.1.1
   ```
2. **Verify CI Workflow**:
   The GitHub Actions workflow under `.github/workflows/dotnet-tool-publish.yml` builds, packs, and publishes the tool to NuGet.org automatically upon tag push.
3. **Install & Test**:
   ```bash
   dotnet tool install --global dejan-skills --version 0.1.1
   dejan-skills --help
   ```

## Repository Hooks & Setup Tools

The repository includes `.githooks/` and tooling setup managed via `dejan-skills tools`. Notably, the post-commit hook under `.github/tools/graphify-openwiki/post-commit.sh` automatically updates OpenWiki documentation and rebuilds Graphify knowledge graphs, including safeguards against duplicate `<!-- OPENWIKI:START/END -->` blocks.

Related concepts: see [/openwiki/architecture/sync-tool.md](/openwiki/architecture/sync-tool.md) for sync tool architecture.
