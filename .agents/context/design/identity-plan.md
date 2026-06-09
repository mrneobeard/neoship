# Identity Plan

## Purpose

Plan identity for NeoShip from the actual C# model in `apps/api/Data.Model`.

Research docs from `~/repos/neo/research/docs/apps/neocloud/book` are useful, but the C# model is the canonical source. Older spec language should conform to the newer C# types and names.

## Canonical Model Inventory

Current identity-related model classes:

| Concern | Canonical C# Type | Notes |
| --- | --- | --- |
| Tenant | `Organization` | Canonical tenant root. |
| User | `User` | Has `OrgId`, status, name, email, roles. |
| User email history | `UserEmail` | Better fit than old `user_email_identities` idea. |
| Password auth | `UserPasswordAuth` | Exists but is effectively empty. |
| Session | `UserSession` | Good base for browser sessions and risk state. |
| User API key | `UserApiKey` | Canonical personal access key model. |
| User API key claims | `UserApiKeyClaim` | Narrow token claims/scopes. |
| User claims | `UserClaim` | Direct user claims. |
| Known networks | `UserKnownNetwork` | Good base for IP risk history. |
| MFA / passkeys | `UserMfaFactor` | Already has passkey/WebAuthn fields. |
| Identity provider link/config | `UserIdentityProvider` | Needs cleanup before SSO work. |
| Group | `Group` | Org-scoped, has members/owners/roles. |
| Role | `Role` | Has users, groups, role claims. |
| Role claims | `RoleClaim` | Canonical permission claim record. |
| Service account | `ServiceAccount` | Org-scoped non-human principal. |
| Service account claims | `ServiceAccountClaim` | Direct claims. |
| Service account API key | `ServiceAccountApiKey` | Canonical machine key model. |
| Service account API key claims | `ServiceAccountApiKeyClaim` | Narrow token claims/scopes. |
| Audit/security events | `AuditEvent` | Should carry auth/security events too. |

## Model Alignment Decisions

### Tenant

- Use `Organization` as the tenant model.
- Keep `TenantMode` and `OrganizationPlan` as the tenancy and packaging primitives.
- Older docs that say `organizations`, `tenants`, or `workspaces` should map to `Organization`.

### User And Email Identity

- Keep `User` as the human identity root.
- Keep `UserEmail` as the canonical email-history and verification record.
- Do not introduce a separate `user_email_identities` table unless `UserEmail` proves insufficient.
- Add missing password and verification metadata to `User` and `UserPasswordAuth` rather than creating parallel identity tables.

### Sessions

- Keep `UserSession` for browser auth and current-session tracking.
- `UserSession` should remain the web/UI source of truth.
- `ClaimsJson` should stay narrow, but it can hold a session claim snapshot.
- Do not store full resolved authorization state there unless the token format is JWE and the snapshot is intentional.

### API Keys, JWT, And Basic Auth

- Keep `UserApiKey` and `ServiceAccountApiKey` as the durable API credential models.
- Add optional short-lived encrypted JWT access tokens as an exchange/output format, not the canonical source of authority.
- Recommendation:
  - Browser UI: `UserSession` cookie.
  - API clients: bearer API key or service account API key.
  - Internal/service delegation: short-lived encrypted JWT or session-backed JWT.
  - Basic auth: disabled by default; if enabled, TLS-only and limited to non-browser API use.
- Do not move primary authorization state into JWT claims unless the JWT is encrypted and intentionally carries a narrow snapshot.
- Resolve current roles/claims from DB or cache.

### MFA And Passkeys

- Keep `UserMfaFactor` as the canonical factor store.
- Use factor type `passkey` and `webauthn_security_key` for WebAuthn.
- Add sign count, attestation metadata, and last-used metadata if missing.
- Passkeys are for browser or native client auth and step-up, not general server-to-server API auth.

### Roles, Groups, Claims

- Keep `Role`, `Group`, `RoleClaim`, `UserClaim`, `ServiceAccountClaim`, `UserApiKeyClaim`, and `ServiceAccountApiKeyClaim`.
- Define permissions as code/module constants first, not as a central permission table.
- Standardize claim naming to permission-style values such as `deployments.create`.
- Store roles, groups, memberships, and grants in DB.
- Let modules/plugins contribute extra permission definitions and seeded roles.
- `GroupRole.cs` is empty today. Fill it or replace the implicit collection-only mapping with explicit join entities.

### Identity Providers

- `UserIdentityProvider` is not clean enough for org-level SSO configuration.
- It currently mixes provider configuration fields with a `UserId` relationship.
- Before OIDC rollout, split the concern into:
  - org/provider configuration
  - user/external identity link
- If naming must stay close to current C#:
  - repurpose `UserIdentityProvider` into a user external-login link
  - add a new org-scoped provider config entity

### Audit And Security Events

- Use `AuditEvent` for auth and security events first.
- Prefer event types like `auth.login.succeeded`, `auth.login.failed`, `auth.passkey.added`, `auth.password.reset.requested`.
- Do not introduce a separate `auth_events` table unless query volume or retention pressure justifies it later.

## Current C# Model Gaps

These are not just design gaps. Some are current code issues.

| Area | Current State | Required Change |
| --- | --- | --- |
| Build health | `Data.Model` does not build | Fix before API work starts. |
| `UserStatus` | Empty file | Implement status enum/value object. |
| `GroupRole` | Empty file | Implement explicit join or remove and configure many-to-many cleanly. |
| `Set<T>` | Referenced, not defined | Replace with `HashSet<T>`. |
| EF attributes | Several files missing `using` directives | Add `System.ComponentModel.DataAnnotations` and `.Schema` as needed. |
| `UserPasswordAuth` | Empty shell | Add hash algorithm, salt/params, hash, upgraded-at, failed-attempt state. |
| `ShipDb` | Missing many `DbSet<>` entries | Register all identity tables and add EF config. |
| `UserIdentityProvider` | readonly struct/property errors and mixed responsibilities | Fix compile issues and split config vs link model. |
| GUID defaults | Several entities default to `Guid.Empty` | Use `Guid.CreateVersion7()` consistently. |
| `OrganizationStatus` | `Deleted` value is inconsistent | Fix value mapping before business logic depends on it. |
| Multi-org membership | `User.OrgId` implies one org | Add additive membership model if multi-org is required. |

## Build Findings

`dotnet build apps/api/Data.Model/NeoShip.Data.Model.csproj` currently fails.

Primary blockers:

- missing `UserStatus`
- undefined `Set<T>` collections that should be `HashSet<T>`
- missing attribute namespace imports in multiple files
- readonly-struct property errors in `UserIdentityProvider`
- incomplete identity model files (`GroupRole.cs`, `UserStatus.cs`)

Identity API work should not start until the canonical model builds.

## Recommended Authentication Shape

### Web

- `UserSession` cookie
- Secure, HttpOnly, SameSite=Lax
- rotate after login, MFA, passkey registration, privilege step-up

### API

- `Authorization: Bearer <user-api-key>`
- `Authorization: Bearer <service-account-api-key>`
- optional: `Authorization: Bearer <encrypted-jwt>` after exchange

### JWT Recommendation

- strongly consider encrypted JWT for delegated API access
- use short TTL: 5 to 15 minutes
- audience-bound and tenant-bound
- include only narrow claims:
  - `iss`
  - `sub`
  - `aud`
  - `exp`
  - `nbf`
  - `jti`
  - `org_id`
  - `scp`
- do not include full groups, roles, or permission lists

### Basic Auth Recommendation

- allowed only over TLS
- disabled by default
- never for browser flows
- prefer for one of these two cases only:
  - API key style credentials for CLI or automation
  - one-shot token exchange endpoint
- do not use long-lived password-based basic auth across the general API surface

## Tenant And Membership Plan

There is tension between the research docs and the current C# model.

- research model assumes multi-org memberships
- current `User.OrgId` points to a single org

Recommended staged approach:

1. keep `User.OrgId` as the home/default org
2. ship identity MVP with one active org per user if necessary
3. add additive `Membership` entity before invitations, enterprise SSO mapping, and org switching
4. do not remove `User.OrgId`; keep it as the primary org/home org even after membership exists

## Entity-Level Implementation Plan

### `Organization`

- keep as tenant root
- add any missing lifecycle fields only if needed by API rollout
- enforce slug uniqueness and tenant-mode semantics

### `User`

- keep current fields
- add:
  - `EmailVerifiedAt`
  - `PasswordChangedAt`
  - `Locale`
  - `Timezone`
  - `Theme`
  - optional `DisplayName` if `Name` becomes login/display overloaded
- finish `UserStatus`

### `UserEmail`

- treat as the canonical email history record
- use `EmailDigest` for keyed lookup
- use `EmailUpcase` and `Email` for current/pending email records
- store verification lifecycle here

### `UserPasswordAuth`

- add:
  - `PasswordHash`
  - `PasswordHashAlgorithm`
  - `PasswordHashParamsJson`
  - `PasswordUpdatedAt`
  - `FailedAttemptCount`
  - `LockedUntil`
  - `MustChangePassword`

### `UserSession`

- keep opaque session token digest
- add/confirm:
  - idle timeout handling
  - absolute timeout handling
  - revoke reason semantics
  - recent-auth marker

### `UserMfaFactor`

- use for TOTP and passkeys
- add:
  - `WebAuthnSignCount`
  - `Aaguid`
  - `LastUsedAt`
  - `ChallengeExpiresAt` only if challenge persistence is stored in DB

### `UserApiKey` / `ServiceAccountApiKey`

- keep durable API key models
- add:
  - public key prefix/id for operator visibility
  - optional `LastUsedIpDigest`
  - creator metadata where missing
  - org scope where needed for user API keys
- claims/scopes stay narrow and additive

### `Role`, `Group`, Claims

- define built-in system roles in canonical seed data
- keep claim tables as the durable permission expression layer
- make group membership and role assignment explicit in EF config

### `ServiceAccount`

- keep org-scoped
- add explicit status field to match planned lifecycle
- treat service accounts as principals with direct claims plus role/group membership

### Identity Provider Model

- add explicit org-level provider configuration entity
- keep user-level external identity link separate
- OIDC first, SAML later

### `AuditEvent`

- use it for both security audit and auth telemetry correlation
- standardize `Type`, `Action`, `TargetType`, `RiskLevel`, `RiskFlagsJson`

## API Surface To Build

Recommended initial API groups:

- `/api/v1/auth/*`
- `/api/v1/me/*`
- `/api/v1/orgs/{org}/service-accounts/*`
- `/api/v1/orgs/{org}/roles/*`
- `/api/v1/orgs/{org}/groups/*`
- `/api/v1/orgs/{org}/identity-providers/*`

Priority auth flows:

1. signup
2. login
3. logout
4. password reset request
5. password reset confirm
6. email verification request/confirm
7. session list/revoke
8. user API key create/revoke/list
9. service account create/key create/revoke
10. TOTP and passkey registration/login
11. OIDC login

## Recommended Delivery Order

### Phase 0

- make `Data.Model` compile
- finish canonical identity entities
- add missing `DbSet<>` and EF config

### Phase 1

- signup/login/logout
- email verification
- password reset
- current-user profile and session management

### Phase 2

- user API keys
- service accounts and service account API keys
- bearer auth middleware
- optional JWT exchange
- optional TLS-only basic auth gate

### Phase 3

- TOTP
- passkeys/WebAuthn
- risk-based step-up auth

### Phase 4

- groups, roles, claims enforcement
- built-in role seeding
- explicit org permission checks

### Phase 5

- OIDC provider config
- SSO login flow
- group/claim mapping

## Success Criteria

- canonical C# identity model builds cleanly
- all auth state is tenant-aware where it should be
- browser auth works with secure opaque sessions
- API auth works with durable API keys and optional short-lived encrypted JWT
- basic auth, if enabled, is TLS-only and tightly scoped
- passkeys work for registration and login
- security events land in `AuditEvent` with request and trace correlation
