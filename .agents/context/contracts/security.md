# Identity Security Contract

## Canonical Rules

- canonical model lives in `apps/api/Data.Model`
- web auth uses `UserSession`
- durable API credentials use `UserApiKey` and `ServiceAccountApiKey`
- auth and security events write to `AuditEvent`

## Authentication Modes

### Browser

- opaque session cookie only
- `Secure`, `HttpOnly`, `SameSite=Lax`
- rotate on login, MFA success, passkey add/login, and privilege step-up

### API

- bearer user API key
- bearer service account API key
- optional short-lived encrypted JWT access token
- passkey-backed login may mint a session or exchange token, but not a standing machine credential

### Organization Auth Policy

- default organizations are permissive so setup does not lock out the initial operator
- organizations may enforce auth policies for SSO, OIDC, SAML, MFA, passkeys, passwords, and recovery options
- users must not be allowed to remove their last usable authentication method unless organization policy explicitly allows it and they have first configured another allowed sign-in method
- self-service auth changes should fail safe rather than creating admin-only recovery situations

### Basic Auth

- disabled by default
- reject if request is not TLS terminated
- never use for browser/UI routes
- if enabled, allow only for:
  - API key style credentials for automation
  - one-shot token exchange/login helper endpoints
- do not support general long-lived password basic auth across the whole API

## JWT Contract

JWT is optional and secondary, not the primary source of authorization.

- prefer encrypted JWT for delegated API access
- if not using JWE, treat JWT as a session-backed access token and resolve claims from session state or cache
- if using JWE, embedded role/claim data is allowed only as a short-lived snapshot
- short TTL only: 5-15 minutes
- exact audience required
- tenant-bound
- revocation-aware via `jti`

Allowed claims:

- `iss`
- `sub`
- `aud`
- `exp`
- `nbf`
- `jti`
- `org_id`
- `scp`
- `amr`
- session id when session-backed JWT is used

Disallowed claims:

- full role list
- full group list
- full permission list
- secret values
- large profile data

## Password Policy

- default hash: Argon2id
- FIPS option: PBKDF2-HMAC-SHA-256 or PBKDF2-HMAC-SHA-512
- minimum length: 12
- maximum length: 256
- support passphrases
- store algorithm, parameters, salt, hash, and upgrade metadata in `UserPasswordAuth`
- rehash on successful login when policy changes

## Email And Recovery

- use `UserEmail` as canonical email history and verification record
- use `EmailDigest` for keyed lookup
- password reset tokens must be one-time, short-lived, and stored hashed
- email verification tokens must be one-time, short-lived, and stored hashed
- never expose whether a password-reset email exists for a public request

## MFA And Passkeys

- `UserMfaFactor` is the factor store
- passkeys are a first-class factor, not optional polish
- TOTP is supported for compatibility
- recovery codes are single-use and stored hashed or encrypted
- high-risk actions may require recent auth or MFA

## Risk Tracking

- use `UserSession`, `UserKnownNetwork`, and `AuditEvent`
- collect:
  - IP digest
  - IP prefix
  - user agent
  - country
  - region
  - ASN
- flag:
  - new IP
  - new country
  - new ASN
  - impossible travel
  - suspicious network/provider
- default reaction is step-up auth, not hard block

## Authorization

- permission values use `resource.action`
- resolve current permissions from DB or short-lived cache
- cache may include session claims as a snapshot, but DB remains source of truth
- do not trust JWT alone for human authorization state
- service accounts can receive direct claims and role/group-derived claims

## Secrets And Encryption

- application-layer encryption for provider secrets and similar sensitive fields
- use slow password-style hashing, such as `PasswordHasher`/Argon2id, for any human-entered or low-entropy secret that must be verified later, including passwords, recovery codes, shared secrets, and similar credentials
- use checksum/digest hashes, such as BLAKE3, only for high-entropy random bearer values where offline guessing is infeasible, such as generated session tokens, reset tokens, verification tokens, and API keys
- hash session tokens, reset tokens, verification tokens, and API keys before storage
- redact secrets from logs, traces, and audit metadata

## Audit Requirements

Write `AuditEvent` for at least:

- signup
- login success/failure
- logout
- password reset request/confirm
- email verification request/confirm
- API key create/revoke
- service account create/update/disable/delete
- service account API key create/revoke
- MFA add/remove/verify failure
- passkey register/use/remove
- identity provider create/update/enable/disable
- role/group/claim changes

Required audit correlation fields:

- `RequestId`
- `TraceId`
- `SpanId`
- `OrgId`
- `UserId` when applicable
- `SessionId` when applicable
- risk metadata when available

## Data Retention

- raw IP retention should be short and policy-driven
- digests and prefixes may be retained longer for anomaly detection
- revoked sessions and revoked API keys remain queryable for audit and abuse defense

## Non-Negotiables Before API Launch

- `Data.Model` builds cleanly
- `UserStatus` exists
- `UserPasswordAuth` stores real hash metadata
- all API credentials are stored hashed
- all auth mutations emit `AuditEvent`
- TLS enforcement exists before any basic auth support is turned on
