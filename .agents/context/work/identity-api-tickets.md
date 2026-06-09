# Identity API Tickets

## ID-000 Model Build Fix

Scope:

- make `apps/api/Data.Model` build

Tasks:

- implement `UserStatus`
- implement or remove `GroupRole`
- replace `Set<T>` with `HashSet<T>`
- add missing `using` directives
- fix readonly struct errors in `UserIdentityProvider`
- fix `OrganizationStatus` deleted value mismatch

Acceptance:

- `dotnet build apps/api/Data.Model/NeoShip.Data.Model.csproj` passes

## ID-001 EF Registration And Mapping

Scope:

- bring all identity entities into `ShipDb`

Tasks:

- add `DbSet<>` for `UserEmail`, `UserPasswordAuth`, `UserClaim`, `RoleClaim`, `ServiceAccountClaim`, `UserMfaFactor`, `UserIdentityProvider`
- configure joins for users/groups/roles/service accounts
- add indexes for email, token digest, session digest, provider lookup

Acceptance:

- migrations can be generated cleanly

## ID-002 Password And Email Model Completion

Scope:

- finish credential and email lifecycle storage

Tasks:

- extend `UserPasswordAuth`
- standardize `UserEmail` for current, pending, previous email records
- add reset-token and verification-token persistence model or equivalent

Acceptance:

- password reset and email verification flows have durable storage

## ID-003 Core Session Auth

Scope:

- signup, login, logout, profile, sessions

Endpoints:

- `POST /api/v1/auth/signup`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/logout`
- `GET /api/v1/me`
- `GET /api/v1/me/sessions`
- `POST /api/v1/me/sessions/{sessionId}/revoke`

Acceptance:

- cookie session works end-to-end
- audit events exist for success and failure paths

## ID-004 Password Reset

Scope:

- recover access safely

Endpoints:

- `POST /api/v1/auth/password-reset/request`
- `POST /api/v1/auth/password-reset/confirm`

Acceptance:

- tokens are hashed, one-time, short-lived
- request endpoint does not reveal account existence

## ID-005 Email Verification

Scope:

- verify contact/login email

Endpoints:

- `POST /api/v1/auth/email-verification/request`
- `POST /api/v1/auth/email-verification/confirm`

Acceptance:

- verified email state is durable and audited

## ID-006 User API Keys

Scope:

- personal API access

Endpoints:

- `GET /api/v1/me/api-keys`
- `POST /api/v1/me/api-keys`
- `POST /api/v1/me/api-keys/{apiKeyId}/revoke`

Acceptance:

- keys stored hashed
- plaintext shown once
- scopes/claims are narrow

## ID-007 Service Accounts

Scope:

- non-human principals

Endpoints:

- `GET /api/v1/orgs/{orgSlug}/service-accounts`
- `POST /api/v1/orgs/{orgSlug}/service-accounts`
- `PATCH /api/v1/orgs/{orgSlug}/service-accounts/{serviceAccountId}`
- `POST /api/v1/orgs/{orgSlug}/service-accounts/{serviceAccountId}/disable`

Acceptance:

- service account lifecycle is org-scoped and audited

## ID-008 Service Account API Keys

Scope:

- machine access keys

Endpoints:

- `GET /api/v1/orgs/{orgSlug}/service-accounts/{serviceAccountId}/api-keys`
- `POST /api/v1/orgs/{orgSlug}/service-accounts/{serviceAccountId}/api-keys`
- `POST /api/v1/orgs/{orgSlug}/service-accounts/{serviceAccountId}/api-keys/{apiKeyId}/revoke`

Acceptance:

- key auth works end-to-end for service accounts

## ID-009 JWT Exchange

Scope:

- optional short-lived delegated token support

Endpoints:

- `POST /api/v1/auth/token-exchange`

Acceptance:

- token is encrypted
- token expires quickly
- scopes do not exceed source actor authority

## ID-010 TLS-Only Basic Auth Gate

Scope:

- optional machine-friendly auth mode

Tasks:

- feature flag basic auth
- enforce TLS check
- restrict allowed endpoints and actor types

Acceptance:

- basic auth cannot be used over non-TLS
- behavior is covered by integration tests

## ID-011 TOTP And Recovery Codes

Scope:

- MFA compatibility path

Endpoints:

- `POST /api/v1/me/mfa/totp/start`
- `POST /api/v1/me/mfa/totp/confirm`
- `POST /api/v1/me/mfa/totp/disable`

Acceptance:

- TOTP and recovery codes work and are audited

## ID-012 Passkeys

Scope:

- phishing-resistant auth

Endpoints:

- `POST /api/v1/me/passkeys/begin-registration`
- `POST /api/v1/me/passkeys/finish-registration`
- `POST /api/v1/auth/passkeys/begin-login`
- `POST /api/v1/auth/passkeys/finish-login`

Acceptance:

- registration and login succeed
- credential replay protections exist

## ID-013 Groups, Roles, And Claims

Scope:

- actual authorization model

Tasks:

- define a module-backed permission registry
- seed built-in roles
- finalize claim vocabulary
- implement group membership changes
- resolve permissions from user, group, service account, and API key claims
- support custom roles built from registry-defined permissions
- allow enterprise modules to add extra permissions and seeded roles

Acceptance:

- permission checks are consistent and cache invalidation works
- OSS and enterprise permission catalogs can coexist without schema changes

## ID-014 Identity Providers

Scope:

- OIDC SSO foundation

Tasks:

- split provider config from user external identity link
- add provider CRUD
- implement OIDC begin/callback
- map claims to local identity

Acceptance:

- one working OIDC provider end-to-end

## ID-015 Observability And Audit

Scope:

- logging, telemetry, tracing, audit normalization

Tasks:

- implement required spans
- emit required metrics
- normalize `AuditEvent.Type` values
- redact logs and traces

Acceptance:

- auth flows are diagnosable without leaking secrets

## ID-016 Test Matrix

Scope:

- close the risk gap

Tests:

- login success/failure
- password reset
- email verification
- session revoke
- API key revoke
- TLS-only basic auth enforcement
- JWT exchange validation
- passkey registration/login
- TOTP setup/verify
- org isolation
- audit event emission
- redaction checks

Acceptance:

- identity API is covered by happy-path, denial-path, and security-path tests
