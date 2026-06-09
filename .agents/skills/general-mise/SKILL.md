---
name: general-mise
description: mise dev environment management, tools, env vars, tasks, mise.toml, version pinning, and project tool setup; use when adding or changing mise configuration.
---

# General Mise

Use this skill for mise-en-place dev environment setup.

## What Mise Does

- Manages project tool versions.
- Loads project environment variables.
- Runs project tasks.
- Uses `mise.toml` or `.mise.toml` depending on project convention.

## Current Repo State

- No root mise config exists today.
- There is a test-only `mise.toml` under `lib/dn/Exec/test`.
- Root task runner today is Cast through `castfile`.

## Rules

- Do not add root mise config without user intent.
- If adding one, pin tool versions that matter for reproducibility.
- Keep secrets out of mise config.
- Prefer env var names already used by the app.
- Do not duplicate Cast tasks unless mise is explicitly becoming the task runner.
- If mise tasks are useful to developers, consider whether they belong in `castfile` instead.

## Common Commands

- `mise doctor` checks setup.
- `mise install` installs configured tools.
- `mise use <tool>@<version>` pins a tool.
- `mise run <task>` runs a configured task.
- `mise env` shows environment export data.

## Good Uses Here

- Pinning Deno or Python/uv if `.agents/scripts` starts depending on them.
- Pinning Node, pnpm, or .NET SDK only if the repo decides mise is the source of truth.
- Local dev environment docs where shell activation matters.

## Review Gate

- New or changed mise config must be user reviewed before commit.
- Explain what tools, env vars, and tasks changed.
