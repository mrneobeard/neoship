# Identity API Design Contract

## Style

- REST/JSON
- `/api/v1` prefix
- camelCase payloads
- OpenAPI generated from source-controlled definitions
- org-scoped routes use `/api/v1/orgs/{orgSlug}/...`

Use the shared response style already described in the research contract.

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

- `GET /api/v1/orgs/{orgSlug}/service-accounts`
- `POST /api/v1/orgs/{orgSlug}/service-accounts`
- `GET /api/v1/orgs/{orgSlug}/service-accounts/{serviceAccountId}`
- `PATCH /api/v1/orgs/{orgSlug}/service-accounts/{serviceAccountId}`
- `POST /api/v1/orgs/{orgSlug}/service-accounts/{serviceAccountId}/disable`
- `POST /api/v1/orgs/{orgSlug}/service-accounts/{serviceAccountId}/enable`
- `GET /api/v1/orgs/{orgSlug}/service-accounts/{serviceAccountId}/api-keys`
- `POST /api/v1/orgs/{orgSlug}/service-accounts/{serviceAccountId}/api-keys`
- `POST /api/v1/orgs/{orgSlug}/service-accounts/{serviceAccountId}/api-keys/{apiKeyId}/revoke`

### Access Control

- `GET /api/v1/orgs/{orgSlug}/roles`
- `GET /api/v1/orgs/{orgSlug}/groups`
- `POST /api/v1/orgs/{orgSlug}/groups`
- `POST /api/v1/orgs/{orgSlug}/groups/{groupId}/members`
- `DELETE /api/v1/orgs/{orgSlug}/groups/{groupId}/members/{principalId}`

### Identity Providers

- `GET /api/v1/orgs/{orgSlug}/identity-providers`
- `POST /api/v1/orgs/{orgSlug}/identity-providers`
- `PATCH /api/v1/orgs/{orgSlug}/identity-providers/{providerId}`
- `POST /api/v1/orgs/{orgSlug}/identity-providers/{providerId}/enable`
- `POST /api/v1/orgs/{orgSlug}/identity-providers/{providerId}/disable`
- `GET /api/v1/auth/sso/{orgSlug}/begin`
- `GET /api/v1/auth/sso/callback`

## Request/Response Semantics

- use `data` envelope for success
- use `error` envelope for failures
- use cursor pagination for mutable lists
- use `filter[...]` and `sort`
- use `Idempotency-Key` on mutating create-style endpoints where retries are likely

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
- scopes on the JWT cannot exceed scopes on the originating key or actor
- encrypted JWT must still map back to current DB actor state for sensitive operations

## Acceptance Rules

- login and reset endpoints are rate-limited
- all auth mutations write `AuditEvent`
- `me` endpoints always resolve current actor and tenant context first
- org-scoped routes never trust an org id in request body
