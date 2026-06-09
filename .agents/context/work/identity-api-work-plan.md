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
- API key login/logout exchange flow
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

- module-defined permission registry and core permission constants
- built-in role seed data
- custom role creation from registry-backed permissions
- group CRUD
- group membership changes
- claim assignment and evaluation
- service account direct claims and group/role membership
- scoped permission model for global, org, and resource access
- enterprise module can contribute extra permissions and seeded roles without affecting OSS

Exit criteria:

- permission checks resolve from DB grants plus module-defined permission registry
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

### Phase 6: Observability And Audit

- structured request logging
- audit event normalization
- OTel traces for auth/session/key/permission flows
- OTel metrics for login, session, MFA, API key, passkey, and revocation paths
- redaction for secrets, tokens, and high-risk identifiers

Exit criteria:

- auth flows are diagnosable without leaking secrets
- request logs, traces, metrics, and audit rows correlate by request/trace ids

## Cross-Cutting Work In Parallel

- security hardening
- audit event normalization
- logging and redaction
- OpenTelemetry spans and metrics
- API/query contracts: language-neutral envelopes, errors, filtering, sorting, paging, expanding, and batch limits
- rate limiting
- tests

## Libraries To Add

Prefer Microsoft packages or widely used .NET-standard packages with regular releases.

### Likely Additions

- `Microsoft.Extensions.Caching.StackExchangeRedis` for distributed cache
- `StackExchange.Redis` for Redis connections
- `Microsoft.AspNetCore.Authentication.JwtBearer` if bearer JWT auth is used directly
- `Microsoft.IdentityModel.JsonWebTokens` for compact JWT/JWE handling if needed
- `OpenTelemetry.Instrumentation.EntityFrameworkCore` for DB spans if we want ORM visibility
- `Fido2NetLib` for WebAuthn/passkey ceremonies if we do not keep passkey logic entirely custom

### Optional Later

- `McMaster.NETCore.Plugins` only if we need runtime plugin loading/unloading instead of compile-time modules

### Usually Already Covered

- `OpenTelemetry.Extensions.Hosting`
- `OpenTelemetry.Exporter.OpenTelemetryProtocol`
- `OpenTelemetry.Instrumentation.AspNetCore`
- `OpenTelemetry.Instrumentation.Http`
- `OpenTelemetry.Instrumentation.Runtime`
- `Serilog.AspNetCore`
- `Serilog.Sinks.Console`
- `Serilog.Sinks.File`

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

## After Identity

Do not start deployment-platform feature work until identity, tenants, and authorization are stable enough to protect privileged operations.

Next work is tracked in:

- `index.md`
- `post-identity-index.md`
- `post-identity-feature-tickets.md`

First follow-on slice:

1. audit and activity foundation
2. jobs and long-running operations foundation
3. secrets, configs, and variables foundation
4. import/export primitive
