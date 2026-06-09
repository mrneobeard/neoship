---
name: neo-observability
description: OpenTelemetry, Serilog, spans, metrics, request context, audit correlation, redaction, and telemetry cardinality; use when changing logs, traces, metrics, or audit correlation.
---

# Neo Observability

Use this skill for logging, tracing, metrics, and audit correlation.

## Required Context

- Read `.agents/context/contracts/otel.md`.
- Read `apps/api/ServiceDefaults/Extensions.cs`.
- Read `apps/api/ApiSvc/RequestContext.cs` and middleware when request correlation changes.

## Current Stack

- Serilog in `apps/api/ApiSvc/Program.cs`.
- OpenTelemetry service defaults in `apps/api/ServiceDefaults`.
- Request context middleware copies request id, trace id, span id, IP, and user agent.

## Span Rules

- Use required span names from the OTel contract for auth operations.
- Add `neocloud.org.id` when tenant-scoped.
- Add `neocloud.user.id` when authenticated.
- Add `neocloud.actor.type` where known.
- Add `error.type` on failures.

## Metric Rules

- Use names from the OTel contract.
- Avoid high-cardinality metric labels.
- Do not use emails, IPs, user agents, token ids, session ids, provider URLs, or raw ids as metric labels.

## Redaction Rules

- Never log or trace passwords, API keys, JWT bodies, reset tokens, verification tokens, passkey payloads, or TOTP secrets.
- Audit events may include ids and risk metadata, but not secret values.
- Keep audit `RequestId` and `TraceId` aligned with active request context.

## Validation

- Test auth/audit paths for expected event creation.
- Review logs/traces for secret leakage and cardinality risks.
