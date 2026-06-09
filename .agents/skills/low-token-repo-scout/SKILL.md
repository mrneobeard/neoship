---
name: low-token-repo-scout
description: Research, planning, repo exploration, token budget, and context gathering; use when investigating this repo, making a plan, or trying to keep answers short and targeted.
---

# Low Token Repo Scout

Use this skill to learn only the context needed for the task.

## Start Here

- Read the nearest `AGENTS.md` first.
- Use `Glob` for file shape and `Grep` for symbols or contracts.
- Prefer targeted reads over whole-directory scans.
- For identity/API work, read `.agents/context/contracts/*` before code.
- For roadmap work, read `.agents/context/design/*` and `.agents/context/work/*`.

## Repo Facts

- Root: mixed .NET, SvelteKit, TypeScript, Aspire.
- API: `apps/api/ApiSvc`.
- EF model: `apps/api/Data.Model`.
- Provider migrations: `apps/api/Data.Model.Sqlite`, `Data.Model.Pgsql`, `Data.Model.Mssql`.
- UI: `apps/ui`.
- AppHost: `apps/apphost`.
- Shared .NET libraries: `lib/dn`.

## Behavior

- Keep user updates short.
- Do not explain obvious tool calls.
- Make the smallest correct plan.
- Avoid speculative architecture unless requested.
- If code should be changed, change it instead of only proposing.
- Stop and ask only when a repo rule or user intent is genuinely ambiguous.

## Output Style

- Lead with the answer.
- Then list changed files or next steps only if useful.
- Mention tests run or why tests were skipped.
