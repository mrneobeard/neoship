# Multi-Database And AppHost Plan

## Purpose

Plan support for:

- SQLite
- PostgreSQL
- SQL Server
- provider-specific EF Core migrations in each provider library
- slightly different provider-specific `DbContext` model overrides
- Aspire TypeScript AppHost that runs API and UI and can switch databases
- API testing with Testcontainers.NET
- later Meilisearch support

This plan is based on the current repo shape, not a greenfield design.

## Current Repo Baseline

Already present:

- `apps/api/Data.Model`
- `apps/api/Data.Model.Sqlite`
- `apps/api/Data.Model.Pgsql`
- `apps/api/Data.Model.Mssql`
- `apps/api/ApiSvc`
- `apps/api/ServiceDefaults`
- `apps/apphost/apphost.mts`
- `apps/ui`

Current state:

- `Data.Model` holds the canonical EF model and `ShipDb`
- provider-specific libraries exist but are basically empty placeholders
- `ApiSvc` is still a minimal sample API
- TypeScript AppHost exists but only has the builder skeleton

## Goals

1. Keep one canonical domain model in `Data.Model`.
2. Keep provider-specific differences small and isolated.
3. Generate and store migrations in the provider-specific libraries.
4. Allow the API and AppHost to switch between SQLite, PostgreSQL, and SQL Server without forking the app.
5. Make provider coverage testable in CI with realistic infrastructure.

## Recommended Architecture

## Canonical Data Layer

Keep `apps/api/Data.Model` as the provider-neutral source of truth.

Responsibilities:

- entities
- shared enums/value objects
- base `ShipDb`
- common `DbSet<>`
- common entity configuration
- provider-neutral indexes and constraints where possible

`ShipDb` should own the common model and expose one provider hook only.

Recommended pattern:

```csharp
public class ShipDb : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShipDb).Assembly);
        ConfigureProviderModel(modelBuilder);
    }

    protected virtual void ConfigureProviderModel(ModelBuilder modelBuilder)
    {
    }
}
```

This matches the requested shape: separate contexts override only slight provider differences.

## Provider-Specific Libraries

Use the existing provider libraries instead of creating new ones:

- `Data.Model.Sqlite`
- `Data.Model.Pgsql`
- `Data.Model.Mssql`

Each provider library should contain:

- reference to `Data.Model`
- provider EF package
- provider-specific context deriving from `ShipDb`
- provider-specific `IEntityTypeConfiguration<>` or model-builder extension methods
- design-time factory for EF migrations
- its own `Migrations/` folder

Recommended contexts:

- `SqliteShipDb : ShipDb`
- `PgsqlShipDb : ShipDb`
- `MssqlShipDb : ShipDb`

Each derived context should override only the provider-specific model tweaks.

## Provider Model Override Scope

Allowed provider-specific differences:

- JSON column mapping
- string column type differences
- date/time precision differences
- index filter syntax differences
- generated/default SQL differences
- concurrency token behavior
- provider-specific extensions such as `jsonb`

Not allowed in provider contexts:

- duplicated entity sets
- duplicated business rules
- duplicated relationships unless the provider forces a real structural difference
- provider-only domain behavior

## Likely Provider Differences

### SQLite

Use for:

- local development
- fast tests
- long-term single-node or small-cluster installs
- self-hosted installs that want low operational overhead

Expected differences:

- JSON stored as text
- limited DDL behavior compared to server databases
- looser type system
- different default value SQL support
- migration operations sometimes need provider-specific workarounds

Recommendation:

- use file-backed SQLite for local dev and Aspire
- support SQLite as a legitimate long-term install option, not just a dev database
- pair long-term SQLite installs with Litestream for continuous backup/restore safety
- use temp-file SQLite for fast integration tests

### PostgreSQL

Use for:

- primary production-grade default for hosted/self-hosted serious installs

Expected differences:

- `jsonb`
- more expressive indexes
- strong support for constraints and filtered queries
- better future fit for richer search metadata and analytics

Recommendation:

- treat PostgreSQL as the most capable relational provider
- prefer it as the baseline for advanced features, while keeping core model portable

### SQL Server

Use for:

- Microsoft-heavy shops
- enterprise Windows/.NET-heavy environments
- teams that already prefer SQL Server operationally or contractually

Expected differences:

- `nvarchar`/`varchar` choices
- `datetime2` precision handling
- filtered index SQL differences
- identity/default SQL differences

Recommendation:

- support SQL Server as a first-class provider for MS shops and teams that prefer it
- avoid model assumptions that only fit SQL Server

## Migrations Strategy

## Rule

Each provider library owns its own EF migrations.

That means:

- SQLite migrations live in `Data.Model.Sqlite/Migrations`
- PostgreSQL migrations live in `Data.Model.Pgsql/Migrations`
- SQL Server migrations live in `Data.Model.Mssql/Migrations`

## Why

The model is mostly shared, but generated migration SQL is provider-specific.

This avoids:

- giant provider conditionals in a single migration set
- fragile multi-provider migration hacks
- runtime confusion about which migrations assembly is authoritative

## Design-Time Factories

Each provider library should include a design-time factory:

- `SqliteShipDbFactory`
- `PgsqlShipDbFactory`
- `MssqlShipDbFactory`

Each factory should:

- build the correct provider options
- point EF tooling at the provider-specific migrations assembly
- use a development-safe local connection string

## Migration Commands

Standardize commands like:

```bash
dotnet ef migrations add InitialIdentity \
  --project apps/api/Data.Model.Sqlite \
  --startup-project apps/api/ApiSvc \
  --context SqliteShipDb
```

```bash
dotnet ef migrations add InitialIdentity \
  --project apps/api/Data.Model.Pgsql \
  --startup-project apps/api/ApiSvc \
  --context PgsqlShipDb
```

```bash
dotnet ef migrations add InitialIdentity \
  --project apps/api/Data.Model.Mssql \
  --startup-project apps/api/ApiSvc \
  --context MssqlShipDb
```

## Runtime Provider Selection

`ApiSvc` should switch providers from configuration.

Recommended config:

```json
{
  "Database": {
    "Provider": "sqlite",
    "ConnectionString": "..."
  }
}
```

Supported values:

- `sqlite`
- `pgsql`
- `mssql`

Recommended registration shape:

- add one composition method such as `AddShipData(configuration)`
- branch on `Database:Provider`
- register the matching derived context
- expose `ShipDb` as the app-facing type

`ApiSvc` should not contain provider-specific model code.

## Migrate-On-Startup vs Migrator

Recommended approach:

- local dev and AppHost: allow automatic migration application
- production: use a dedicated migrator process/tool

Recommended addition:

- `apps/api/DbMigrator`

Responsibilities:

- load the same provider config as `ApiSvc`
- resolve the correct provider-specific context
- call `Database.Migrate()`
- exit cleanly

This makes AppHost orchestration simpler and gives production a clean migration entrypoint.

## Aspire TypeScript AppHost Plan

## AppHost Resources

Use the existing `apps/apphost/apphost.mts`.

Resource plan:

- API via `addProject(...)`
- UI via `addExecutable(...)` running `npm run dev` in `apps/ui`
- SQLite as a local file-backed configuration resource
- PostgreSQL via container resource
- SQL Server via container resource
- optional `DbMigrator` resource before `ApiSvc`

The generated TypeScript SDK already exposes `addProject`, `addExecutable`, and `addContainer`, so this plan fits the current tool surface.

## Switching Databases In AppHost

Use one explicit selector, not three separate apphost files.

Recommended mechanism:

- `DB_PROVIDER=sqlite|pgsql|mssql`

AppHost behavior:

- `sqlite`: create/store a file path, pass SQLite connection string to API
- `sqlite`: create/store a file path, pass SQLite connection string to API, and make Litestream integration easy to add beside it
- `pgsql`: start a PostgreSQL container, pass connection string/reference to API
- `mssql`: start a SQL Server container, pass connection string/reference to API

If Aspire profile support is comfortable later, add profiles such as:

- `sqlite`
- `pgsql`
- `mssql`

But the first implementation can be environment-driven.

## UI In AppHost

The UI already has an `npm run dev` script.

Recommended AppHost executable shape:

- command: `npm`
- working directory: `apps/ui`
- args: `run dev`

Then:

- expose HTTP endpoint
- pass API base URL via environment variable
- wait for API only if the UI truly requires backend readiness

## API In AppHost

Recommended AppHost project resource:

- project path: `apps/api/ApiSvc/NeoShip.ApiSvc.csproj`

Environment variables passed from AppHost:

- `Database__Provider`
- `ConnectionStrings__Default` or `Database__ConnectionString`
- `ASPNETCORE_ENVIRONMENT`
- OTEL settings already compatible with `ServiceDefaults`

## AppHost Delivery Order

1. choose provider
2. provision database resource if needed
3. run migrator
4. start API
5. start UI

## Testing Plan

## Test Project Layout

Recommended projects:

- `apps/api/ApiSvc.Tests`
- optional later: `apps/api/Data.Model.Tests`

`ApiSvc.Tests` should use:

- xUnit
- `Microsoft.AspNetCore.Mvc.Testing`
- Testcontainers.NET

## Provider Matrix

### SQLite

Use:

- temp-file DB for integration tests
- no container needed

Coverage:

- startup
- migrations apply
- CRUD and query semantics
- auth/session flows later

### PostgreSQL

Use:

- Testcontainers.NET PostgreSQL container

Coverage:

- migrations apply from `Data.Model.Pgsql`
- provider-specific JSON/index behavior
- API integration tests through `WebApplicationFactory`

### SQL Server

Use:

- Testcontainers.NET SQL Server container

Coverage:

- migrations apply from `Data.Model.Mssql`
- string/date/index/provider-specific behavior
- API integration tests through `WebApplicationFactory`

## Test Layers

### Layer 1: Model And Migration Smoke Tests

Per provider:

- context boots
- migrations assembly resolves
- `Database.Migrate()` succeeds on empty DB

### Layer 2: API Integration Tests

Per provider:

- API starts
- health endpoint passes
- basic create/read/write flows work
- transaction/error behavior is sane

### Layer 3: Contract Tests For Provider Differences

Examples:

- JSON fields round-trip correctly
- uniqueness rules match expectations
- date/time precision is acceptable
- case sensitivity assumptions are explicit

## CI Recommendation

On PR:

- run SQLite integration tests always
- run PostgreSQL integration tests always
- run SQL Server integration tests if build time is acceptable

If SQL Server becomes too slow for every PR:

- keep smoke coverage on PR
- run full SQL Server matrix on main/nightly

## Risks

## Current Model Risk

`Data.Model` still does not build today. The multi-database work depends on fixing that first.

## Drift Risk

If provider-specific contexts start accumulating lots of logic, the providers will drift. Keep overrides small and explicit.

## Migration Drift Risk

Separate migrations are correct, but they must be generated from the same shared model. Avoid provider-only business changes.

## AppHost Complexity Risk

Trying to make AppHost dynamically support too many advanced options too early can slow delivery. Start with one provider selector and a small number of scripts.

## Later Search Plan

Add a later follow-up for Meilisearch.

Scope for later:

- container in AppHost
- search abstraction in API
- sync/indexing pipeline
- rich search endpoints and UI support

Do not block relational provider work on Meilisearch.
