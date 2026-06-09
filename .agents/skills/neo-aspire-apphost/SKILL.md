---
name: neo-aspire-apphost
description: Aspire TypeScript AppHost, API/UI orchestration, database provider switching, DB_PROVIDER, containers, and local dev runtime; use when editing apps/apphost or runtime orchestration.
---

# Neo Aspire AppHost

Use this skill for `apps/apphost` and local orchestration.

## Required Context

- Read `.agents/context/design/multi-db-apphost-plan.md`.
- Read `apps/apphost/apphost.mts`.
- Read `apps/apphost/aspire.config.json`.

## AppHost Rules

- Keep one AppHost with provider selection, not separate apphosts per database.
- Preferred selector: `DB_PROVIDER=sqlite|pgsql|mssql`.
- API project path: `../api/ApiSvc/NeoShip.ApiSvc.csproj` from `apps/apphost`.
- UI executable should run the UI dev script from `apps/ui`.
- Pass API base URL to UI through environment when needed.

## Database Plan

- SQLite: local file-backed config for development.
- PostgreSQL: container resource when provider is `pgsql`.
- SQL Server: container resource when provider is `mssql`.
- Later migrator project should run before API for production-style migration flow.

## Editing Rules

- Preserve TypeScript `NodeNext` module behavior.
- Do not duplicate `builder.build().run()`.
- Keep provider-specific environment variables explicit: `Database__Provider` and connection string settings.

## Validation

- Run `pnpm --dir apps/apphost run build` after AppHost TypeScript changes.
- Run or smoke-check `pnpm --dir apps/apphost run dev` when orchestration behavior changes and dependencies are available.
