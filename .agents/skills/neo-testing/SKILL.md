---
name: neo-testing
description: Tests, builds, validation commands, xUnit, Vitest, Playwright, Testcontainers, and after-change verification; use when adding tests or verifying changes.
---

# Neo Testing

Use this skill for validation and test strategy.

## Always

- After every code change, run relevant tests before finishing.
- Fix build errors, warnings, style issues, and test failures caused by the change.
- Prefer the smallest relevant test command first.
- Mention any skipped tests and why.

## Root Commands From `castfile`

- Build all .NET: `dotnet build neoship.slnx`.
- Test all .NET: `dotnet test neoship.slnx`.
- Unit tests: `dotnet test neoship.slnx --filter-trait Category=Unit`.
- Integration tests: `dotnet test neoship.slnx --filter-trait Category=Integration`.
- UI build: `pnpm --dir apps/ui run build`.
- UI check: `pnpm --dir apps/ui run check`.
- AppHost run: `pnpm --dir apps/apphost run dev`.
- AppHost build: `pnpm --dir apps/apphost run build`.

## API And EF Tests

- API tests live in `apps/api/ApiSvc.Tests`.
- Tests use xUnit traits such as `Category=Unit`, `Category=Integration`, `Category=Auth`, and `Category=Migration`.
- SQLite integration tests can run without containers.
- PostgreSQL and SQL Server provider tests should use Testcontainers when implemented.

## UI Tests

- Unit/browser tests use Vitest and Playwright provider.
- E2E tests use Playwright.
- `apps/ui/AGENTS.md` prefers Vite+ validation: `vp check` and `vp test`.

## When Adding Tests

- Test behavior, not implementation detail.
- Cover success, failure, and security edge cases for auth.
- For EF provider work, cover migration application and provider-specific semantics.
- For UI, include accessibility-relevant states and mobile/desktop behavior when practical.
