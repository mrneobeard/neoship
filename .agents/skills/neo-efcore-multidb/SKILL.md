---
name: neo-efcore-multidb
description: Entity Framework Core, ShipDb, migrations, SQLite, PostgreSQL, SQL Server, and provider-specific model rules; use when editing EF entities, DbContext, or migrations.
---

# Neo EFCore Multidb

Use this skill for EF Core and database provider work.

## Required Context

- Read `.agents/context/design/multi-db-apphost-plan.md`.
- Read `.agents/context/design/identity-plan.md` for identity model changes.
- Read current `apps/api/Data.Model/ShipDb.cs` and the touched entities.

## Architecture Rules

- `apps/api/Data.Model` is the canonical provider-neutral model.
- SQLite migrations live in `apps/api/Data.Model.Sqlite/Migrations`.
- PostgreSQL migrations live in `apps/api/Data.Model.Pgsql/Migrations`.
- SQL Server migrations live in `apps/api/Data.Model.Mssql/Migrations`.
- Provider-specific libraries should only contain provider context, provider tweaks, factories, and migrations.
- Do not duplicate business rules in provider-specific contexts.

## Provider Differences Allowed

- JSON column mapping.
- String column type differences.
- Date/time precision differences.
- Filtered index SQL differences.
- Generated/default SQL differences.
- Concurrency token behavior.
- Provider extensions such as PostgreSQL `jsonb`.

## Model Rules

- Keep common relationships and indexes in `ShipDb` where possible.
- Use `UseSnakeCaseNamingConvention()` in runtime/test options unless a task says otherwise.
- Prefer `Guid.CreateVersion7()` through existing factory helpers when entity ids need generated values.
- Be careful with SQLite in-memory tests: keep the connection open for database lifetime.

## Migration Commands

- SQLite: `dotnet ef migrations add <Name> --project apps/api/Data.Model.Sqlite --startup-project apps/api/ApiSvc --context SqliteShipDb`.
- PostgreSQL: `dotnet ef migrations add <Name> --project apps/api/Data.Model.Pgsql --startup-project apps/api/ApiSvc --context PgsqlShipDb`.
- SQL Server: `dotnet ef migrations add <Name> --project apps/api/Data.Model.Mssql --startup-project apps/api/ApiSvc --context MssqlShipDb`.

## Validation

- Build `apps/api/Data.Model/NeoShip.Data.Model.csproj` after model changes.
- Run migration or integration tests relevant to the provider touched.
- For cross-provider changes, test SQLite first and plan PostgreSQL/SQL Server coverage.
