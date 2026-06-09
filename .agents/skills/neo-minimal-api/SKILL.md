---
name: neo-minimal-api
description: ASP.NET Minimal API, route groups, typed results, /api/v1 contracts, request/response shapes; use when adding or changing API endpoints.
---

# Neo Minimal API

Use this skill for `apps/api/ApiSvc` endpoint work.

## Required Context

- Read `.agents/context/contracts/api-design.md` before changing endpoint behavior.
- Read `.agents/context/contracts/security.md` for auth-sensitive endpoints.
- Read existing endpoint files in `apps/api/ApiSvc/Endpoints`.

## Endpoint Style

- Use REST/JSON.
- Use `/api/v1` prefix.
- Use route groups such as `routes.MapGroup("/api/v1/auth")`.
- Prefer `TypedResults` and explicit `Results<...>` return types.
- Include `CancellationToken` on async handlers.
- Use `[FromBody]` for body records where existing style does.
- Keep endpoint request/response records near the endpoint group unless reuse is real.

## Contract Rules

- Success should move toward a `data` envelope when the contract requires it.
- Failures should move toward an `error` envelope with shared error codes.
- Org-scoped routes use `/api/v1/orgs/{orgSlug}/...`.
- Org-scoped routes must not trust org id from request body.
- Mutable create-style endpoints should consider `Idempotency-Key` where retries are likely.

## Security Rules

- Never log passwords, tokens, API keys, JWT bodies, reset tokens, verification tokens, TOTP secrets, or passkey payloads.
- Auth mutations must write `AuditEvent`.
- Login and reset endpoints need rate-limit awareness.
- Basic auth is TLS-only and tightly scoped if enabled.

## Validation

- Run API tests for endpoint changes.
- Add or update tests for status codes, typed result paths, auth behavior, and audit events.
