# Identity API Design Contract

## Style

- REST/JSON
- `/api/v1` prefix
- camelCase payloads
- OpenAPI 3.1 generated from source-controlled definitions
- IAM completion requires OpenAPI generation/validation for IAM routes
- current-organization routes infer org context from the actor and use clean nouns such as `/api/v1/roles`, `/api/v1/groups`, and `/api/v1/service-accounts`

Use `api-contract.md` for response envelopes, error envelopes, HTTP semantics, idempotency, async operations, concurrency, and compatibility rules.
Use `querying.md` for filter, search, sort, pagination, expand, batch, and query validation rules.

## Authentication Shapes

Supported request auth:

- session cookie
- `Authorization: Bearer <user-api-key>`
- `Authorization: Bearer <service-account-api-key>`
- `Authorization: Bearer <short-lived-encrypted-jwt>`
- optional `Authorization: Basic ...` only over TLS when explicitly enabled

## Naming Conventions

Conform public names to canonical C# types where practical.

- personal API credential routes use `api-keys`, not generic `api-tokens`
- service account machine credentials use `api-keys`
- passkey routes sit under `me/passkeys`
- session routes sit under `me/sessions`

## Core Endpoints

### Public Auth

- `POST /api/v1/auth/signup`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/logout`
- `POST /api/v1/auth/password-reset/request`
- `POST /api/v1/auth/password-reset/confirm`
- `POST /api/v1/auth/email-verification/request`
- `POST /api/v1/auth/email-verification/confirm`
- `POST /api/v1/auth/passkeys/begin-login`
- `POST /api/v1/auth/passkeys/finish-login`
- `POST /api/v1/auth/token-exchange` optional

### Current User

- `GET /api/v1/me`
- `PATCH /api/v1/me`
- `GET /api/v1/me/sessions`
- `POST /api/v1/me/sessions/{sessionId}/revoke`
- `GET /api/v1/me/api-keys`
- `POST /api/v1/me/api-keys`
- `POST /api/v1/me/api-keys/{apiKeyId}/revoke`
- `GET /api/v1/me/passkeys`
- `POST /api/v1/me/passkeys/begin-registration`
- `POST /api/v1/me/passkeys/finish-registration`
- `POST /api/v1/me/passkeys/{factorId}/rename`
- `POST /api/v1/me/passkeys/{factorId}/revoke`
- `POST /api/v1/me/mfa/totp/start`
- `POST /api/v1/me/mfa/totp/confirm`
- `POST /api/v1/me/mfa/totp/disable`

### Service Accounts

- `GET /api/v1/service-accounts`
- `POST /api/v1/service-accounts`
- `GET /api/v1/service-accounts/{serviceAccountId}`
- `PATCH /api/v1/service-accounts/{serviceAccountId}`
- `POST /api/v1/service-accounts/{serviceAccountId}/disable`
- `POST /api/v1/service-accounts/{serviceAccountId}/enable`
- `GET /api/v1/service-accounts/{serviceAccountId}/api-keys`
- `POST /api/v1/service-accounts/{serviceAccountId}/api-keys`
- `POST /api/v1/service-accounts/{serviceAccountId}/api-keys/{apiKeyId}/revoke`

### Access Control

- `GET /api/v1/org/{orgId}/auth/policy`
- `PATCH /api/v1/org/{orgId}/auth/policy`
- `GET /api/v1/roles`
- `POST /api/v1/roles`
- `GET /api/v1/groups`
- `POST /api/v1/groups`
- `POST /api/v1/groups/{groupId}/members`
- `DELETE /api/v1/groups/{groupId}/members/{principalId}`

### Identity Providers

- `GET /api/v1/users/identity-providers`
- `POST /api/v1/users/identity-providers`
- `PATCH /api/v1/users/identity-providers/{providerId}`
- `POST /api/v1/users/identity-providers/{providerId}/enable`
- `POST /api/v1/users/identity-providers/{providerId}/disable`
- `GET /api/v1/auth/sso/{orgSlug}/begin`
- `GET /api/v1/auth/sso/callback`

Identity provider creation supports `preset` values `github`, `google`, and `microsoft`. A preset fills provider type and standard HTTPS metadata endpoints; explicit `issuerUrl` or `metadataJson` may override preset defaults when needed. If both `preset` and `providerType` are provided, `providerType` must match the preset.

## Request/Response Semantics

- use `data` envelope for success
- use `error` envelope for failures
- validate request data in the route/store layer before database mutation and return one aggregate validation error containing all detected field violations
- FluentValidation may be used for non-trivial validators, but keep simple validators local when that avoids unnecessary abstraction
- use cursor pagination for mutable lists
- use `filter[...]` and `sort`
- use `Idempotency-Key` on mutating create-style endpoints where retries are likely
- list responses use the `pagination` envelope from `api-contract.md`
- public payloads are language-neutral JSON, not framework-specific result shapes
- IAM completion requires canonical `api-contract.md` envelopes across IAM routes; this is not deferred cleanup

## Error Codes

Use existing shared error vocabulary first:

- `unauthenticated`
- `permission_denied`
- `tenant_not_found`
- `not_found`
- `conflict`
- `validation_failed`
- `rate_limited`

Add auth-specific detail codes under `error.details.code` as needed:

- `invalid_credentials`
- `password_reset_token_invalid`
- `email_verification_token_invalid`
- `mfa_required`
- `step_up_required`
- `api_key_revoked`
- `api_key_expired`

## Basic Auth Rules

If basic auth is enabled:

- require TLS or trusted TLS termination
- reject with `unauthenticated` if scheme is used over non-TLS
- do not allow browser-oriented session endpoints to rely on basic auth
- prefer basic auth only for API key or service account key based machine access

## JWT Exchange Rules

If JWT exchange exists:

- JWT is short-lived only
- refresh happens by re-auth or API key exchange, not long-lived refresh tokens unless separately designed
- if JWT is not JWE, it should be session-backed and resolved through session state or cache
- if JWT is JWE, it may carry a narrow role/claim snapshot, but still expires quickly and remains revocation-aware
- scopes on the JWT cannot exceed scopes on the originating key, session, or actor
- encrypted JWT must still map back to current DB actor state for sensitive operations

## Passkey Use

- passkeys are for browser or native client login and step-up
- passkey assertions can start an auth flow or mint a session/token
- do not treat passkeys as a generic direct bearer credential for server-to-server API calls

## Acceptance Rules

- login and reset endpoints are rate-limited
- all auth mutations write `AuditEvent`
- `me` endpoints always resolve current actor and tenant context first
- org-scoped routes never trust an org id in request body
- routes with heavy logic, security decisions, or key product flows must have route-level tests, not only store/unit tests
- route-level API tests should use `Microsoft.AspNetCore.TestHost` with in-memory dependencies when possible; this verifies Minimal API routing, binding, DI, auth resolution, cookies, and status codes without requiring a full external host
- service-account bearer access must have route-level tests for allowed read, missing permission, and disabled parent account cases; store tests alone are not enough because the route auth helper combines bearer parsing, key authentication, and permission resolution
- service-account write routes must have route-level coverage showing bearer service accounts are rejected and human-user writes create audit events
- service-account claim routes follow the same rule: bearer service accounts may read with scoped permission, but claim mutations require a human user and must emit audit events
- service-account API-key claim routes, permission registry, role reads, and identity-provider reads must be covered for scoped bearer access; their mutation routes must reject service-account bearers even when the bearer has matching write claims
- tests should move toward shared fixtures for seed data; in-code fixtures are fine initially, YAML/data-file fixtures are acceptable when they reduce duplication
- release-grade validation should include automated E2E coverage against each supported DB provider
