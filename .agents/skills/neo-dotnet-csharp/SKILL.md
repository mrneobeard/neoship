---
name: neo-dotnet-csharp
description: .NET, C#, net11.0, xUnit, libraries, and application code; use when editing C# outside narrow EF or Minimal API tasks.
---

# Neo Dotnet CSharp

Use this skill for C# implementation work in `apps/api` and `lib/dn`.

## Project Shape

- Solution file: `neoship.slnx`.
- API app: `apps/api/ApiSvc`.
- Test app: `apps/api/ApiSvc.Tests`.
- Shared libraries: `lib/dn/*/src`.
- Library tests: `lib/dn/*/test`.
- Tests use xUnit v3 with Microsoft Testing Platform.

## Implementation Rules

- Keep changes local to the feature.
- Prefer one clear function over new helper layers.
- Use `CancellationToken` on async API and DB paths.
- Use `ConfigureAwait(false)` in library code when existing nearby code does.
- Do not introduce backward compatibility unless required by persisted data or external consumers.
- For secrets and auth, prefer secure primitives and constant-time comparisons.

## Async And Results

- Use `Task<T>` for regular async operations.
- Use `ValueTask<T>` only when it is already part of the API shape or clearly avoids allocation on a hot path.
- Avoid sync-over-async and blocking waits in request paths.

## Validation

- Build the touched project first when the full solution may be slow.
- Run relevant tests after every code change.
- Use `dotnet test neoship.slnx` for broad changes.
