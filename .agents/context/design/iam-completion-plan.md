# IAM Completion Plan

## Outcome

IAM is complete when the backend/API supports multi-org identity, auth policy, sessions, MFA/passkeys, provider login, API keys, service accounts, roles/groups/permissions, deletion flows, audit plumbing, mail delivery, route contracts, and provider validation according to the contracts in `.agents/context/contracts`.

UI is explicitly out of scope for IAM completion.

## Working Rules

- Keep slices small and vertical.
- Update docs when a decision changes behavior.
- Add route-level tests for security decisions, core product flows, and heavy logic.
- Run focused `dotnet build` and `dotnet test` after each code slice.
- SQLite/current-provider tests are acceptable for fast loops; SQLite, PostgreSQL, and SQL Server validation is required before PRs/releases or DB-logic changes.
- Commit only after relevant build/test pass.

## Phase 1: Stabilize Current Route-Test Work

- Commit current passing route-test and auth-policy work.
- Keep `Microsoft.AspNetCore.TestHost` for Minimal API route coverage.
- Refactor `RouteTests` into reusable fixtures if it keeps growing.
- Preserve service-account bearer rules:
  - read routes may allow scoped bearer service accounts.
  - mutation routes are human-user-only until non-human audit actor semantics exist.

Verification:

- `dotnet build apps/api/ApiSvc.Tests/NeoShip.ApiSvc.Tests.csproj`
- `dotnet test apps/api/ApiSvc.Tests/NeoShip.ApiSvc.Tests.csproj`

## Phase 2: Multi-Org Membership And Invites

- Add membership model with per-org roles/groups/permissions.
- Keep `User.OrgId` as home/current org.
- Add org switch/current-org behavior.
- Add explicit email invites with 7-day default expiry.
- Invites may carry pending role/group assignments.
- Apply pending role/group assignments only after invite acceptance and first login into the org.
- Add built-in owner/admin/member role semantics.
- Add route tests for membership, invite accept/revoke/expiry, and org switching.

Likely areas:

- `apps/api/Data.Model`
- `apps/api/ApiSvc/Stores`
- `apps/api/ApiSvc/Endpoints/OrgEndpoints.cs`
- provider migrations for SQLite/PostgreSQL/SQL Server

## Phase 3: Auth Policy And Step-Up Completion

- Finish org auth-policy fields and routes.
- Enforce policy across signup, login, passkey registration/login, reset, verification, API-key login, OAuth2/OIDC login.
- Add org MFA policy modes:
  - off
  - admins/owners only
  - all members
- Required MFA enrollment needs recovery codes or another approved recovery method.
- Add recent-auth/passkey/MFA step-up for sensitive mutations unless disabled globally or by org policy.
- Add route tests for policy denial and step-up success/failure.

## Phase 4: Mail Delivery

- Add mailer abstraction.
- Add SMTP provider.
- Add logging/console provider for dev.
- Add programmatic test-capture provider.
- Wire password reset, email verification, invite emails, and deletion confirmation emails through mailer.
- Defer vendor-specific providers like SendGrid, Resend, and Postmark to plugins/providers.

## Phase 5: Provider Login

- Finish OIDC login and callback with policy-controlled auto-provisioning.
- Add OAuth2 login for common providers first:
  - GitHub
  - Google
  - Microsoft
- Use OIDC metadata for WorkOS/Auth0 where possible.
- Defer full generic OAuth2 claim-mapping engine.
- Auto-provisioning requires verified email and must create user, membership, and external identity link together.
- Exact provider-subject links take precedence over email matching.
- SAML is deferred to enterprise SSO.

## Phase 6: API Keys And Rotation

- Add configurable global/org API-key expiry defaults.
- Allow non-expiring keys only by explicit policy override.
- Plaintext API key is shown once at creation and never retrievable later.
- User API keys are narrowable by direct claims/scopes.
- User API key creation can optionally snapshot current user roles/perms point-in-time.
- Service-account API keys follow the same narrowable plus optional snapshot model.
- Add explicit rotation flow.
- Rotation only allowed if old key has not expired.
- Old-key handling modes:
  - revoke immediately
  - keep current `ExpiresAt`
  - move `ExpiresAt` earlier
- Never extend old key expiry during rotation.
- Add route tests for user and service-account key create/revoke/rotate/snapshot/narrowing.

## Phase 7: Permissions Registry And Built-In Roles

- Builtin permission registry is enough for IAM completion.
- Admins assign registered permissions only; no arbitrary freeform permission creation.
- Plugin/module permission ingestion is pre-go-live follow-up.
- Seed built-in owner/admin/member roles.
- Make role/group/list endpoints use query contract.
- Add tests for built-in role capabilities and permission assignment.

## Phase 8: Deletion And Retention

- Add user deletion/anonymization flows.
- Add org deletion request/confirmation flow for org admins.
- Default deletion is soft-delete with access revocation.
- Soft-delete records carry scheduled hard-delete/anonymize date.
- Timing is configurable globally, by org policy, and per deletion request override.
- Audit logs are retained for breach, abuse, and unlawful-activity investigation unless later compliance policy says otherwise.
- Org data export is a pre-go-live todo, not IAM completion.

## Phase 9: Audit Plumbing

- Treat audit as reusable platform plumbing.
- Keep IAM completion to write/store events, not search/report APIs.
- Ensure all auth/security mutations emit `AuditEvent`.
- Preserve provider-switch/enrichment design for later apps.
- Add route/store tests for missing audit on sensitive mutations.

## Phase 10: Caching And Rate Limiting

- Cache source of truth remains DB.
- In-memory cache is default.
- Add Aspire/local env switch to use Redis for local/testing distributed behavior.
- Redis distributed caching is selectable, but distributed rate limiting is deferred.
- Add in-process rate limiting for login, reset, invite, and public security-sensitive endpoints.
- Add invalidation for session/API-key/role/group/claim/policy changes.

## Phase 11: Canonical API Contract And OpenAPI

- Convert IAM route responses to `api-contract.md` envelopes.
- Use error envelopes for failures.
- Add cursor pagination/filter/sort helpers for mutable lists.
- OpenAPI generation/validation is required for IAM routes.
- Route payloads stay language-neutral JSON.

## Phase 12: CLI Bootstrap/Admin Tooling

- Add CLI tooling for first org/admin setup.
- CLI should use public APIs where practical and obey caller access.
- CLI should support common admin operations needed before UI exists.
- GUI/Avalonia admin tool is deferred.

## Phase 13: Cross-Org Administration Pre-Go-Live

- Default users/service accounts are per-org.
- Roles/groups/permissions remain per-org grants.
- Before go-live, add administrative/owner org model for cross-org operations.
- Platform admin org can create special cross-install service accounts.
- Primary/customer org can own/link subsidiary orgs.
- Cross-org service accounts may run only against owned/linked orgs.
- API keys must be able to represent actor allowed org scope without exceeding actor access.

## Phase 14: Validation Matrix

- Fast loop:
  - API test project
  - SQLite/current-provider tests
- Before commits touching DB logic:
  - generate/update SQLite, PostgreSQL, SQL Server migrations
  - run available provider checks for touched areas when practical
- Before PRs/releases:
  - SQLite, PostgreSQL, SQL Server migration/E2E automation
  - full `dotnet build neoship.slnx`
  - full `dotnet test neoship.slnx`

## Deferred Explicitly

- UI screens.
- Basic Auth.
- SAML enterprise SSO.
- Standalone JWE delegation.
- Full risk engine for known device/network/anomaly tracking.
- Audit search/report APIs.
- Domain auto-join.
- Multiple verified login emails and account linking.
- Vendor-specific mail providers in core.
- Plugin/module permission ingestion, unless needed before go-live.
- Org data export API/jobs, but it is required before go-live.
