# Multi-Database And AppHost Tickets

## DB-000 Fix Canonical Model First

Scope:

- make `apps/api/Data.Model` build cleanly before adding provider-specific runtime support

Acceptance:

- `dotnet build apps/api/Data.Model/NeoShip.Data.Model.csproj` passes

## DB-001 Refactor Base Context

Scope:

- keep one shared `ShipDb`
- add one provider override hook for model tweaks

Tasks:

- move common configuration into provider-neutral model configuration classes
- add `ConfigureProviderModel(ModelBuilder)` hook
- ensure `ShipDb` owns all common `DbSet<>`

Acceptance:

- base context is provider-neutral

## DB-002 Implement SQLite Provider Context

Scope:

- fill `apps/api/Data.Model.Sqlite`

Tasks:

- add `SqliteShipDb`
- add design-time factory
- add SQLite migrations folder
- add SQLite-specific model tweaks only where needed
- document SQLite as valid for long-term installs, not only dev/test
- leave a clean integration point for Litestream backup/restore support

Acceptance:

- SQLite migrations can be created and applied

## DB-003 Implement PostgreSQL Provider Context

Scope:

- fill `apps/api/Data.Model.Pgsql`

Tasks:

- add `PgsqlShipDb`
- add design-time factory
- add PostgreSQL migrations folder
- map JSON/text/index differences cleanly

Acceptance:

- PostgreSQL migrations can be created and applied

## DB-004 Implement SQL Server Provider Context

Scope:

- fill `apps/api/Data.Model.Mssql`

Tasks:

- add `MssqlShipDb`
- add design-time factory
- add SQL Server migrations folder
- map date/string/index differences cleanly
- document SQL Server as primarily for MS shops and teams that prefer it

Acceptance:

- SQL Server migrations can be created and applied

## DB-005 Standardize EF Tooling Commands

Scope:

- make provider-specific migration generation easy and repeatable

Tasks:

- document `dotnet ef` commands for each provider
- ensure migrations assembly routing is correct

Acceptance:

- developers can add migrations without guessing flags

## DB-006 Add Runtime Provider Selection To ApiSvc

Scope:

- make `ApiSvc` choose SQLite, PostgreSQL, or SQL Server from config

Tasks:

- add configuration contract
- register the correct provider-specific context
- keep app code consuming the shared `ShipDb` abstraction

Acceptance:

- API starts against all three providers from config only

## DB-007 Add DbMigrator Tool

Scope:

- create a dedicated migration runner

Tasks:

- add `apps/api/DbMigrator`
- load provider config
- apply pending migrations for the chosen provider

Acceptance:

- migrations can be run outside API startup

## APP-001 Build TypeScript AppHost Resource Graph

Scope:

- make `apps/apphost/apphost.mts` actually run the stack

Tasks:

- add API project resource
- add UI executable resource
- add provider selector
- wire API/UI environment variables

Acceptance:

- AppHost starts API and UI together

## APP-002 Add SQLite AppHost Path

Scope:

- support SQLite in AppHost

Tasks:

- create/store SQLite file path
- pass SQLite connection string to migrator and API
- design the path so Litestream can later run beside SQLite without reworking the stack

Acceptance:

- AppHost runs API+UI against SQLite

## APP-003 Add PostgreSQL AppHost Path

Scope:

- support PostgreSQL in AppHost

Tasks:

- add PostgreSQL container resource or equivalent
- pass connection string/reference to migrator and API

Acceptance:

- AppHost runs API+UI against PostgreSQL

## APP-004 Add SQL Server AppHost Path

Scope:

- support SQL Server in AppHost
- target MS shops and users who prefer SQL Server operations

Tasks:

- add SQL Server container resource or equivalent
- pass connection string/reference to migrator and API

Acceptance:

- AppHost runs API+UI against SQL Server

## APP-005 Add AppHost Convenience Scripts

Scope:

- make provider switching simple for developers

Tasks:

- add scripts or profile docs for `sqlite`, `pgsql`, `mssql`

Acceptance:

- provider switching is one command

## TEST-001 Create ApiSvc Test Project

Scope:

- add API integration test project

Tasks:

- add xUnit
- add `Microsoft.AspNetCore.Mvc.Testing`
- add Testcontainers.NET

Acceptance:

- test host boots and can run provider-backed tests

## TEST-002 Add SQLite Integration Tests

Scope:

- cover local provider path

Tasks:

- use temp-file SQLite
- run migrations
- hit API endpoints through `WebApplicationFactory`
- add at least one test path that matches long-lived file-backed SQLite behavior

Acceptance:

- SQLite test suite passes in CI

## TEST-003 Add PostgreSQL Integration Tests

Scope:

- cover PostgreSQL provider path

Tasks:

- spin up PostgreSQL with Testcontainers.NET
- apply PostgreSQL migrations
- run API integration tests

Acceptance:

- PostgreSQL test suite passes in CI

## TEST-004 Add SQL Server Integration Tests

Scope:

- cover SQL Server provider path

Tasks:

- spin up SQL Server with Testcontainers.NET
- apply SQL Server migrations
- run API integration tests

Acceptance:

- SQL Server test suite passes in CI

## TEST-005 Add Provider Migration Smoke Tests

Scope:

- verify migrations are valid for all providers

Tasks:

- migrate empty DB for SQLite
- migrate empty DB for PostgreSQL
- migrate empty DB for SQL Server

Acceptance:

- all providers can reach latest migration cleanly

## TEST-006 CI Matrix

Scope:

- run provider coverage automatically

Tasks:

- run SQLite always
- run PostgreSQL always
- decide PR vs nightly strategy for SQL Server based on runtime

Acceptance:

- provider support is enforced by automation

## SEARCH-001 Meilisearch Later

Scope:

- backlog item for richer search later

Tasks:

- add Meilisearch container/resource in AppHost
- add search abstraction in API
- add indexing pipeline from relational data
- add future search endpoints and UI integration

Acceptance:

- tracked as explicit future work, not forgotten

## OPS-001 Litestream Later

Scope:

- backlog item for durable SQLite backup/restore support

Tasks:

- add Litestream deployment pattern for SQLite installs
- document restore workflow
- decide whether AppHost should run Litestream as sidecar/container/process for local and self-hosted scenarios

Acceptance:

- tracked as explicit future work for long-term SQLite installs
