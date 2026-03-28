# dejan-skills

Small .NET tool for syncing Copilot skills and prompt aliases from a GitHub repository into another repository.

For this public source repository, no token is required.

Set `GITHUB_TOKEN` only when syncing from a private source repository.

## Commands

- `dejan-skills list`
- `dejan-skills init`
- `dejan-skills bootstrap`
- `dejan-skills update`

Notes:

- `bootstrap` is prompt-first and syncs prompts by default. Add `--with-skills` to include skills too.
- `update` prunes stale previously managed files by default. Add `--no-prune` to disable.
- Sync targets include `github`, `claude`, and `opencode` by default.
- Use `--platforms github,claude,opencode` to customize platform outputs.
- Claude skill copies are written with `user-invocable: true` during sync.

Run `dejan-skills --help` for usage details.