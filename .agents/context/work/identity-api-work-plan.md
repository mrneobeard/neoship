# Identity API Work Plan

## Goal

Implement identity API from the canonical C# model, while fixing the model so it is buildable and complete enough to support auth, tenants, service accounts, groups, roles, claims, identity providers, and passkeys.

## Delivery Order

### Phase 0: Canonical Model Hardening

Must happen first.

- make `Data.Model` compile
- implement `UserStatus`
- implement `GroupRole` or replace with explicit supported mapping
- replace `Set<T>` with `HashSet<T>`
- add missing attribute namespace imports
- fix `UserIdentityProvider` compile problems
- fix `OrganizationStatus.Deleted` mismatch
- register missing `DbSet<>` entries in `ShipDb`
- extend `UserPasswordAuth` with real password-hash fields

Exit criteria:

- `dotnet build apps/api/Data.Model/NeoShip.Data.Model.csproj` passes
- all identity tables are represented in EF

### Phase 1: Core Auth API

- signup
- login
- logout
- current user profile
- session list and revoke
- email verification request/confirm
- password reset request/confirm

Exit criteria:

- full browser session auth works
- reset and verification tokens are one-time and hashed
- audit events exist for all auth mutations

### Phase 2: API Credential API

- list/create/revoke user API keys
- create/list/revoke service account API keys
- bearer auth middleware for both key types
- optional encrypted JWT exchange endpoint
- optional TLS-only basic auth path if enabled

Exit criteria:

- API keys are durable and hashed in storage
- JWT, if enabled, is short-lived and encrypted
- basic auth, if enabled, is rejected over non-TLS

### Phase 3: MFA And Passkeys

- TOTP start/confirm/disable
- passkey begin/finish registration
- passkey begin/finish login
- recovery code generation and use
- recent-auth and step-up flow

Exit criteria:

- passkey registration and login work end-to-end
- high-risk actions can require MFA or recent auth

### Phase 4: Access Control API

- built-in role seed data
- group CRUD
- group membership changes
- claim assignment and evaluation
- service account direct claims and group/role membership

Exit criteria:

- permission checks are DB-backed
- permission cache invalidates correctly

### Phase 5: Identity Providers

- org provider config model
- OIDC metadata discovery and validation
- SSO begin/callback endpoints
- user external-identity link
- claim/group mapping

Exit criteria:

- OIDC login works for one provider
- provider secrets are encrypted
- SSO changes are audited

## Cross-Cutting Work In Parallel

- security hardening
- audit event normalization
- logging and redaction
- OpenTelemetry spans and metrics
- rate limiting
- tests

## Risks

### Model Risk

The current C# model does not build. This is the first real blocker.

### Tenancy Risk

Current `User.OrgId` is simpler than the older multi-org research model. Decide early whether MVP is single-home-org or whether additive membership must be in scope now.

### SSO Modeling Risk

`UserIdentityProvider` is not ready as-is for clean org-level OIDC configuration.

### JWT Scope Risk

Encrypted JWT can help with delegated access, but should not become the durable source of authorization truth.

## Recommended Immediate Next Sprint

1. fix the canonical model so it builds
2. add missing password/session/email verification fields
3. define API contracts for signup/login/reset/session/profile
4. implement auth middleware and session handling
5. implement core auth endpoints and tests
