# Identity OpenTelemetry Contract

## Required Spans

- `auth.signup`
- `auth.login`
- `auth.logout`
- `auth.password.reset.request`
- `auth.password.reset.confirm`
- `auth.email.verify.request`
- `auth.email.verify.confirm`
- `auth.passkey.begin_registration`
- `auth.passkey.finish_registration`
- `auth.passkey.begin_login`
- `auth.passkey.finish_login`
- `auth.totp.start`
- `auth.totp.confirm`
- `auth.session.refresh`
- `auth.step_up`
- `auth.api_key.create`
- `auth.api_key.revoke`
- `service_account.create`
- `service_account.api_key.create`
- `identity_provider.create`
- `identity_provider.callback`

## Required Span Attributes

- `neocloud.org.id` when tenant-scoped
- `neocloud.user.id` when authenticated
- `neocloud.actor.type`
- `error.type` on failures

Identity-specific attributes:

- `neocloud.auth.method`
- `neocloud.auth.result`
- `neocloud.auth.mfa_required`
- `neocloud.auth.step_up.trigger`
- `neocloud.auth.provider_type`
- `neocloud.api_key.type`
- `neocloud.jwt.exchange`

## Metric Names

- `neocloud.auth.login.duration`
- `neocloud.auth.login.total`
- `neocloud.auth.signup.total`
- `neocloud.auth.password_reset.total`
- `neocloud.auth.passkey.total`
- `neocloud.auth.totp.total`
- `neocloud.auth.step_up.total`
- `neocloud.auth.session.active`
- `neocloud.auth.api_key.total`
- `neocloud.auth.jwt_exchange.total`

## Propagation

- HTTP request span must flow through DB work and any email/job dispatch
- `AuditEvent.RequestId` and `AuditEvent.TraceId` must match active request context
- async email delivery for verification/reset should continue trace where practical

## Redaction

Never put these in span attributes or events:

- password
- API key
- JWT body
- reset token
- verification token
- passkey challenge response payload
- TOTP secret

## Cardinality Guardrails

Allowed in traces:

- ids for actor, org, session, target when needed

Not allowed in metrics:

- emails
- IPs
- user agents
- token ids
- session ids
- provider URLs
