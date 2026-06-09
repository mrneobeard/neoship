---
name: neo-identity-security
description: Identity, auth, sessions, API keys, JWT, Basic auth, passkeys, MFA, audit events, and security contracts; use when touching authentication or authorization.
---

# Neo Identity Security

Use this skill for identity and security behavior.

## Required Contracts

- `.agents/context/contracts/security.md`.
- `.agents/context/contracts/api-design.md`.
- `.agents/context/contracts/otel.md`.
- `.agents/context/design/identity-plan.md`.

## Canonical Model

- Tenant root: `Organization`.
- Human identity: `User`.
- Email lifecycle: `UserEmail`.
- Password auth: `UserPasswordAuth`.
- Browser sessions: `UserSession`.
- User API credentials: `UserApiKey`.
- Service account credentials: `ServiceAccountApiKey`.
- MFA/passkeys: `UserMfaFactor`.
- Audit/security events: `AuditEvent`.

## Auth Rules

- Browser auth uses opaque session cookies.
- Session cookies must be `Secure`, `HttpOnly`, and `SameSite=Lax`.
- Durable API access uses bearer user API keys or service account API keys.
- Optional JWTs are short-lived exchange outputs, not the primary auth source.
- Basic auth is disabled by default, TLS-only, and never for browser flows.
- Passkeys are login/step-up factors, not generic machine credentials.

## Secret Rules

- Store session tokens, reset tokens, verification tokens, and API keys hashed.
- Never log or trace raw passwords, API keys, JWT bodies, reset tokens, verification tokens, passkey challenge responses, or TOTP secrets.
- Use constant-time comparison for secret digests.
- Public password reset requests must not reveal whether the email exists.

## Audit Rules

- Auth mutations must write `AuditEvent`.
- Include request and trace correlation when available.
- Record login success/failure, logout, password reset, email verification, API key create/revoke, service-account changes, MFA/passkey changes, provider changes, and role/group/claim changes.

## Acceptance Checks

- Verify tenant scope.
- Verify revocation behavior.
- Verify audit event creation.
- Verify no secret leaks in logs, traces, responses, or test output.
