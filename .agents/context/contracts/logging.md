# Identity Logging Contract

## Streams

- `api`
- `audit`

Identity should not invent a separate log format. It should use structured logs plus `AuditEvent` for durable security events.

## API Log Fields

Every identity request log should carry:

- timestamp
- level
- request id
- trace id
- span id
- route template
- method
- status code
- duration ms
- org id when resolved
- user id or service account id when resolved
- auth mode
- error code when failed

## Audit Log Fields

Backed by `AuditEvent`.

- `Timestamp`
- `Type`
- `Action`
- `OrgId`
- `UserId`
- `TargetType`
- `TargetId`
- `RequestId`
- `SessionId`
- `TraceId`
- `SpanId`
- `IpDigest`
- `CountryCode`
- `Region`
- `Asn`
- `RiskLevel`
- `RiskFlagsJson`
- `DataJson` redacted

## Required Audit Event Types

- `auth.signup`
- `auth.login.succeeded`
- `auth.login.failed`
- `auth.logout`
- `auth.password-reset.requested`
- `auth.password-reset.completed`
- `auth.email-verification.requested`
- `auth.email-verification.completed`
- `auth.api-key.created`
- `auth.api-key.revoked`
- `auth.passkey.added`
- `auth.passkey.used`
- `auth.passkey.removed`
- `auth.totp.enabled`
- `auth.totp.disabled`
- `auth.step-up.required`
- `auth.step-up.succeeded`
- `auth.step-up.failed`
- `identity-provider.created`
- `identity-provider.updated`
- `identity-provider.enabled`
- `identity-provider.disabled`
- `service-account.created`
- `service-account.updated`
- `service-account.disabled`
- `service-account.api-key.created`
- `service-account.api-key.revoked`

## Redaction Rules

Never log plaintext:

- passwords
- password hashes
- API keys
- JWT bodies
- client secrets
- reset tokens
- verification tokens
- passkey credential payloads
- TOTP secrets
- recovery codes

Log digests, ids, prefixes, and redacted metadata only.

## Retention

- API logs: operational retention
- audit logs: longer security retention
- auth failure and auth mutation events must remain searchable by request id, actor, org, and time range

## Minimum Implementation Rule

No identity endpoint is complete until it emits both:

- a normal structured request log
- the correct `AuditEvent` where the action is security-relevant
