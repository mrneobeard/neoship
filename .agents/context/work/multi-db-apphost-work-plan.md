# Multi-Database And AppHost Work Plan

## Goal

Support SQLite, PostgreSQL, and SQL Server with:

- one canonical shared EF model
- provider-specific contexts and migrations
- an Aspire TypeScript AppHost that can switch databases
- API integration testing with Testcontainers.NET

## Phase 0: Prerequisite Cleanup

- fix `Data.Model` build errors
- finish incomplete model files
- stabilize the shared `ShipDb`

Exit criteria:

- canonical data model builds cleanly

## Phase 1: Shared EF Model Structure

- move common model configuration into provider-neutral configuration classes
- add provider override hook on `ShipDb`
- ensure all common `DbSet<>` are registered once

Exit criteria:

- shared model is ready for provider-specific derived contexts

## Phase 2: Provider Implementations

- implement `SqliteShipDb`
- implement `PgsqlShipDb`
- implement `MssqlShipDb`
- add design-time factories
- generate migrations in each provider library

Exit criteria:

- each provider can create and apply migrations independently

## Phase 3: Runtime Wiring

- add provider selection to `ApiSvc`
- add config contract for provider and connection string
- add `DbMigrator`

Exit criteria:

- API and migrator can target any supported provider by configuration

## Phase 4: Aspire TypeScript AppHost

- wire API project into AppHost
- wire UI executable into AppHost
- add SQLite path
- add PostgreSQL container path
- add SQL Server container path
- add provider switch
- leave room for Litestream alongside SQLite for durable self-hosted installs

Exit criteria:

- one AppHost can run API + UI against any supported database

## Phase 5: Test Matrix

- create `ApiSvc.Tests`
- add SQLite integration tests
- add PostgreSQL integration tests with Testcontainers.NET
- add SQL Server integration tests with Testcontainers.NET
- add migration smoke tests for all providers

Exit criteria:

- provider support is covered by automated tests

## Phase 6: CI Enforcement

- run SQLite and PostgreSQL on every PR
- run SQL Server on PR or nightly depending on runtime cost

Exit criteria:

- provider compatibility is not manual or aspirational

## Later Follow-Up

- add Meilisearch apphost resource and API search abstraction later
- add Litestream support path for SQLite backup/restore orchestration
- do not block relational provider support on search work
