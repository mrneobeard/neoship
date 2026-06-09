---
name: general-dotnet-backend
description: General .NET backend, C#, DI, async, configuration, APIs, data access, and production service patterns; use for broad .NET backend work before applying repo-specific Neo rules.
---

# General Dotnet Backend

Adapted from skills found on skills.sh:

- `wshobson/agents@dotnet-backend-patterns`
- `github/awesome-copilot@dotnet-best-practices`

Use this for broad .NET backend judgment. If it conflicts with `neo-code-style`, `neo-dotnet-csharp`, `neo-minimal-api`, or `neo-efcore-multidb`, the Neo skill wins.

## Backend Defaults

- Keep service boundaries simple.
- Use dependency injection lifetimes deliberately: singleton for stateless shared services, scoped for request/database work, transient only when cheap and state-free.
- Prefer strongly typed options for nontrivial configuration.
- Use structured logging with stable property names.
- Avoid exceptions for normal control flow.
- Preserve cancellation through request, database, and IO paths.

## Async Rules

- Use async all the way through request and IO paths.
- Avoid `.Result`, `.Wait()`, and blocking waits.
- Use parallel awaits only for independent operations.
- Use `ValueTask` only when API shape or hot-path profiling justifies it.
- In reusable libraries, follow nearby `ConfigureAwait(false)` style.

## Data Access

- Keep queries explicit and measurable.
- Avoid accidental N+1 queries.
- Prefer projections for read models when full entities are not needed.
- Keep transactions small and scoped to the mutation.
- Do not hide important query shape behind unnecessary repository layers.

## Project Override

- This repo does not prefer primary constructors today.
- This repo requires XML docs for public, internal, and protected API symbols.
- This repo uses xUnit v3, not MSTest.
- Package versions belong in `Directory.Packages.props`.
