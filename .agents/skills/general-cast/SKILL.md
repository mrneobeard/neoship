---
name: general-cast
description: Cast task runner from github.com/frostyeti/cast, castfile tasks, subcommands, dependencies, environment, trusted remote tasks, and developer workflow automation.
---

# General Cast

Use this skill for Cast task runner work.

## Source

- Cast project: `github.com/frostyeti/cast`.
- Repo task file: `castfile`.

## Current Repo Shape

- Root `castfile` id: `neo-ship`.
- Existing subcommands: `aspire`, `build`, `dotnet`, `ui`, `audit`, `lint`.
- Existing tasks cover .NET build/test/format, UI build/check/format/audit, Aspire run/build, and package audit.

## Task Rules

- Add Cast tasks when the workflow is useful to developers, not just the agent.
- Keep task names namespaced with existing subcommands.
- Every task needs a clear `desc`.
- Prefer simple `uses: shell` for straightforward commands.
- Use `needs` for dependencies instead of manually chaining repeated prerequisite work.
- Keep commands safe, explicit, and repo-root relative.
- Do not add remote task sources unless the user asks and trust rules are explicit.

## Useful Cast Concepts

- `subcmds` expose namespaces as command groups.
- `needs` runs dependencies before a task.
- `env` sets task environment.
- `paths` prepends tools to `PATH`.
- Task-level help or desc should make `--help` useful.
- Context suffix routing can pick context-specific variants, such as provider or environment variants.

## When To Promote Agent Scripts

- If `.agents/scripts` helper is useful after this session, add a Cast task.
- If it is only for agent investigation, keep it in `.agents/scripts`.
- If a Cast task calls a script, make the script path and default arguments obvious.

## Review Gate

- New Cast tasks must be user reviewed before commit.
- Summarize what the task runs, what files it reads/writes, and why it belongs in developer workflow.
