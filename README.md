# skills

Reusable Copilot skills and prompt aliases, plus a small .NET tool for syncing them into other repositories.

## Contents

- `.github/skills`: reusable skill definitions and bundled assets
- `.github/prompts`: dot-prefixed prompt aliases that expose the workflows in chat
- `src/Dejan.Skills.Tool`: a packable .NET tool that can list, initialize, and update prompts and skills from this repository into another git repository

Default sync targets now include multiple tool ecosystems:

- `.github` (GitHub Copilot)
- `.claude` (Claude)
- `.opencode` (OpenCode)

## Dotnet Tool

The sync tool is packaged as a .NET tool named `dejan-skills`.

### Maintainer Commands

Quick day-to-day commands:

```bash
# 1) Trigger CI publish for a version
git tag v0.1.1 && git push origin v0.1.1

# 2) Install that version from NuGet (after workflow succeeds)
dotnet tool install --global dejan-skills --version 0.1.1

# 3) Verify tool command
dejan-skills --help

# 4) Sync to GitHub + Claude + OpenCode layouts
dejan-skills init --target /path/to/your/repo
```

### Install From Published Package

```bash
dotnet tool install --global dejan-skills
```

Install a specific version:

```bash
dotnet tool install --global dejan-skills --version 0.1.1
```

Update to latest:

```bash
dotnet tool update --global dejan-skills
```

### Commands

List the available content in the source repository:

```bash
dejan-skills list
```

Bootstrap prompt-first setup for a new repository:

```bash
dejan-skills bootstrap
```

Bootstrap and include skills as well:

```bash
dejan-skills bootstrap --with-skills
```

No token is required for this public repository.

If you use a private fork or private source repository, set `GITHUB_TOKEN`:

```bash
export GITHUB_TOKEN=your_pat_with_repo_read_access
dejan-skills list
```

Initialize the current repository with all prompts and skills from this repo:

```bash
dejan-skills init
```

Initialize a target repository path explicitly:

```bash
dejan-skills init --target /path/to/your/repo
```

Sync only selected platform layouts if needed:

```bash
dejan-skills init --target /path/to/your/repo --platforms github,claude
```

Initialize from a local clone of the source repository instead of downloading from GitHub:

```bash
dejan-skills init \
	--target /path/to/your/repo \
	--source-path /path/to/local/skills-clone \
	--repo https://github.com/dejandjenic/skills
```

Initialize only selected items:

```bash
dejan-skills init \
	--skills dejan-workflow-coding-assistant,dejan-kira-mcp-ticket-ops \
	--prompts dejan.workflow-coding-assistant,dejan.kira-mcp-ticket-ops
```

Update a repository later using the manifest created during `init`:

```bash
dejan-skills update --target /path/to/your/repo
```

By default, `update` now prunes stale previously managed files that were removed upstream.

Disable pruning if needed:

```bash
dejan-skills update --target /path/to/your/repo --no-prune
```

Overwrite existing files when syncing:

```bash
dejan-skills update --target /path/to/your/repo --force
```

### What `init` Writes

The tool copies selected items into the target repository under:

- `.github/skills`
- `.github/prompts`
- `.claude/skills`
- `.claude/prompts`
- `.opencode/skills`
- `.opencode/prompts`

It also writes a manifest file at `.github/dejan-skills.json` so `update` can repeat the sync settings later.

### Defaults

- Source repository: `dejandjenic/skills`
- Source ref: `main`
- Included content: prompts and skills
- Included platforms: github, claude, opencode
- Target path: current working directory

### Notes

- Prompts in this repo reference the matching skills, so the default behavior syncs both.
- `bootstrap` is prompt-first and includes prompts by default. Use `--with-skills` to include skills during bootstrap.
- Skills are hidden from the slash menu in this repository and are meant to support the prompt aliases.
- The tool copies new upstream prompts and skills automatically on `update` when no filters are pinned in the manifest.
- `update` removes stale previously managed files by default, based on `.github/dejan-skills.json`.
- For private source repositories, the tool reads `GITHUB_TOKEN` and sends it as a bearer token to GitHub's archive API.
- `--source-path` is useful for local testing or syncing from a checked-out clone when remote API access is not available.

## CI Publishing

This repo includes [dotnet-tool-publish.yml](.github/workflows/dotnet-tool-publish.yml), which runs on semver tags (`v1.2.3` or `1.2.3`) and can:

- pack the .NET tool package
- upload built `.nupkg` files as workflow artifacts
- create a GitHub release and attach the packages
- optionally publish to NuGet.org when `NUGET_API_KEY` is configured

Versioning behavior:

- Package version is derived from the git tag.
- Example: tag `v0.1.1` or `0.1.1` produces package version `0.1.1`.
- You do not need to update version in [Dejan.Skills.Tool.csproj](src/Dejan.Skills.Tool/Dejan.Skills.Tool.csproj).

## Release Checklist

Use this checklist for each new tool release.

1. Create and push a release tag

```bash
git tag v0.1.1
git push origin v0.1.1
```

2. Verify GitHub Actions publish run

- Open Actions and confirm [dotnet-tool-publish.yml](.github/workflows/dotnet-tool-publish.yml) succeeded for tag `v0.1.1`.
- Confirm release assets include a `.nupkg` package.
- If `NUGET_API_KEY` is configured, confirm package published to NuGet.org.

3. Verify install on a clean environment

From NuGet.org (if published):

```bash
dotnet tool install --global dejan-skills --version 0.1.1
dejan-skills --help
```

4. Verify real sync behavior quickly

```bash
dejan-skills list
dejan-skills bootstrap --target /tmp/dejan-skills-smoke
```

5. Rollback guidance

- If release has issues, publish a new patch version (for example `0.1.2`) instead of replacing an existing package.
- Keep the broken tag for audit trail, but communicate the fixed version as the supported one.