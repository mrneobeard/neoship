# Identity Telemetry Contract

## Purpose

Track health, abuse, and adoption of identity features without leaking secrets or high-cardinality user data.

## Required Metric Families

### Authentication

- login attempts
- login success/failure
- signup success/failure
- password reset requests/completions
- email verification requests/completions

### Sessions

- active sessions
- session revocations
- session expiry count
- step-up auth triggers

### MFA

- TOTP setup success/failure
- passkey registration success/failure
- passkey login success/failure
- MFA challenge failure count

### API Credentials

- user API key creations/revocations
- service account API key creations/revocations
- JWT exchange success/failure if implemented
- basic auth usage count if enabled

### Security Signals

- new IP detections
- new country detections
- new ASN detections
- impossible travel detections
- suspicious-network detections

## Label Rules

Allowed labels:

- auth method
- result
- org id
- token type
- provider type
- step-up trigger
- stable error type

Do not label metrics with:

- email
- user name
- raw IP
- user agent
- token id
- session id
- provider URL

## Event Correlation

- metrics should line up with `AuditEvent` volumes
- request logs, traces, and `AuditEvent` must share request/trace correlation

## Alerts Worth Adding Early

- login failure spike
- password reset request spike
- JWT exchange failure spike
- passkey registration failure spike
- high rate of `step_up_required`
- abnormal increase in revoked API keys
