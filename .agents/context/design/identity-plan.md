# Identity Plan

## Purpose

Plan identity for NeoShip from the actual C# model in `apps/api/Data.Model`.

Research docs from `~/repos/neo/research/docs/apps/neocloud/book` are useful, but the C# model is the canonical source. Older spec language should conform to the newer C# types and names.

IAM completion is backend/API-first. UI screens are deferred until the project, server, SSH, remote script, and local/remote Docker Compose foundations exist, followed by a deliberate product-design pass.

IAM completion includes CLI tooling for bootstrap/admin operations, including first org/admin setup. A GUI/Avalonia admin tool is deferred. The CLI should use the public APIs where practical and obey the caller's access.

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
- IAM completion includes self-service profile update and verified email-change flows, not only login/reset verification.
- IAM completion keeps one primary login email per user; `UserEmail` tracks history, pending changes, and verification state.
- Multiple verified login emails and account linking are deferred, but future account linking should support multi-org switching and safe identity merging.
- IAM completion requires user/org deletion or anonymization flows in addition to suspension and credential revocation.
- Deletion defaults to soft-delete with access revocation.
- Soft-delete records must carry a scheduled hard-delete/anonymize date.
- Hard-delete/anonymize timing is configurable globally, by org policy, and by an override supplied at deletion time.
- Org admins may self-service org deletion after an explicit confirmation flow.
- Org deletion follows the same soft-delete then later hard-delete/anonymize model.
- Org data export is a required pre-go-live todo, not an IAM-completion requirement; audit logs are retained for breach, abuse, and unlawful-activity investigation unless a later legal/compliance policy says otherwise.

### Sessions

- Keep `UserSession` for browser auth and current-session tracking.
- `UserSession` should remain the web/UI source of truth.
- `ClaimsJson` should stay narrow, but it can hold a session claim snapshot.
- Do not store full resolved authorization state there unless the token format is JWE and the snapshot is intentional.

### API Keys, JWT, And Basic Auth

- Keep `UserApiKey` and `ServiceAccountApiKey` as the durable API credential models.
- API keys are single-org scoped by default for IAM completion.
- Before go-live, API keys must be able to represent the actor's allowed org scope when the actor has multi-org or administrative access; keys must never exceed the user/service account's own accessible org set.
- API keys use a configurable default expiration from global/org settings.
- Non-expiring API keys are allowed only through explicit override when policy permits.
- User API keys must support narrow scopes/claims so a key can be less powerful than the user.
- User API key creation may optionally snapshot all current user roles/permissions at creation time; that snapshot is point-in-time and does not automatically grow with later user grants.
- Service-account API keys follow the same model: narrowable scopes/claims plus optional point-in-time snapshot of current service-account permissions.
- IAM completion requires explicit API-key rotation flows; create-new/revoke-old alone is not enough.
- API keys can be rotated only while the old key has not expired.
- Rotation must support old-key handling modes: revoke immediately, keep existing `ExpiresAt`, or move `ExpiresAt` earlier.
- Rotation must never extend the old key's `ExpiresAt` later than its current value.
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
- IAM completion requires built-in owner, admin, and member role semantics for org management decisions such as invites, deletion, and policy changes.
- Define permissions from builtin modules/plugins, then ingest them into an assignable registry for custom roles and augmented existing roles.
- Admins assign registered permissions; they should not create arbitrary freeform permission keys through IAM admin routes.
- Registry persistence may be a table updated by builtin module migrations or plugin/module install/uninstall hooks.
- IAM completion requires builtin permission registry only; plugin/module permission ingestion can be pre-go-live follow-up.
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
- Current staged implementation keeps `UserIdentityProvider` as the org-scoped provider config and adds `UserExternalIdentity` for the user-to-provider subject link; this avoids blocking OIDC route work on a larger model split while preserving the split in behavior.
- OIDC login should support policy-controlled auto-provisioning on first successful SSO login when the provider/org allows it and the external identity has a verified email.
- Auto-provisioning must create the user, membership, and external identity link together; exact provider-subject links still take precedence over email matching.
- IAM completion requires OIDC and OAuth2 provider login flows.
- OAuth2 completion targets common provider presets first, such as GitHub, Google, and Microsoft, plus minimal configurable endpoints/scopes where needed.
- WorkOS/Auth0 should use OIDC metadata when possible.
- A fully generic OAuth2 claim-mapping engine is deferred.
- SAML is deferred to later enterprise SSO work.

### Organization Auth Policy

- Keep auth-policy switches on `Organization` for now: password, passkey, OIDC SSO, SAML SSO, require SSO, and self-service external identity unlink.
- Defaults are permissive so a new/default organization does not lock out the initial operator.
- Self-service unlink checks policy and refuses to remove the last usable sign-in method; this fails safe and avoids creating admin-only recovery situations.
- Add explicit org-admin API routes for reading/updating policy because provider lifecycle routes alone do not show the effective sign-in policy.
- Enforce policy in the store layer as well as route tests: password login returns an explicit method-not-allowed result when blocked, passkey begin/finish returns no auth result, and OIDC begin/callback returns no SSO result when OIDC is disabled.
- Re-check SSO policy at callback time because policy can change after the authorization challenge is issued.

### Route Test Infrastructure

- Use `Microsoft.AspNetCore.TestHost` for route-level API tests.
- Reason: direct handler/store tests miss route binding, DI resolution, auth cookie/bearer behavior, response status mapping, and `Set-Cookie` behavior.
- Keep route tests focused on security decisions and core flows; use in-memory SQLite and fake external providers/token validators unless the test is specifically provider/E2E validation.

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
2. IAM completion includes true multi-org membership, invites, and org switching rather than deferring them
3. add additive `Membership` entity before enterprise SSO mapping and org switching
4. do not remove `User.OrgId`; keep it as the primary org/home org even after membership exists
5. invitations are explicit email invites for IAM completion; verified-domain auto-join rules are deferred
6. invites may include pending role/group assignments, but those assignments are applied only after invite acceptance and the user's first login into the org
7. default invite expiration is 7 days

### Cross-Org Administration

- Default users and service accounts are scoped by org membership; roles, groups, and permissions are assigned per organization.
- Default service accounts belong to exactly one organization.
- Before go-live, add an administrative/owner organization model for legitimate cross-org operations.
- The platform administrative org can create special service accounts that manage across the installation.
- A primary/customer org can own or link subsidiary orgs.
- Cross-org service accounts created by a primary org may run only against orgs that primary org owns or is explicitly linked to manage.
- UI can later make repeated per-org role assignment easier, but the authorization model remains per-org grants.
- Do not allow arbitrary cross-org service accounts without the administrative/owner-org relationship; this is a privileged feature, not default IAM behavior.

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
- `/api/v1/service-accounts/*`
- `/api/v1/roles/*`
- `/api/v1/groups/*`
- `/api/v1/users/identity-providers/*`

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
