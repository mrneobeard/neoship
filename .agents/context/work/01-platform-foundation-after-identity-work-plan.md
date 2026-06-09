# 01 Platform Foundation After Identity Work Plan

## Goal

Finish the safety foundation needed before deployment features: audit, jobs, secrets/config, redaction, and import/export primitives.

## Prerequisites

- identity/session APIs complete enough to resolve actors
- tenant/org context complete enough to scope every record
- authorization checks complete enough for privileged actions
- `api-contract.md`, `querying.md`, `security.md`, `caching.md`, `logging.md`, and `otel.md` are active contracts

## Phase 1: Audit And Activity

- normalize audit action names
- define actor, tenant, target, action, risk, request id, trace id, IP, user agent fields
- add append-only audit write path
- add audit list/detail endpoints
- add query filters for actor, action, target, risk, time range, and search
- add audit redaction rules

Exit criteria:

- every identity/authorization mutation emits audit
- cross-tenant audit reads are denied
- audit list follows `querying.md`
- audit rows can correlate to logs/traces by request id and trace id

## Phase 2: Jobs And Long-Running Operations

- add job and job event model
- add job queue/lease abstraction for SQLite and PostgreSQL first
- add job status/detail/event endpoints
- add SSE stream endpoint for job events
- add cancellation model
- add retry/backoff model
- add idempotency key storage for job-backed mutations

Exit criteria:

- any operation expected to exceed request timeout can return `202 Accepted` with a job resource
- workers cannot process jobs without tenant context
- job input, output, and logs are redacted
- job lease behavior is covered by tests

## Phase 3: Secrets, Configs, And Variables

- add `config_items`, versions, usage refs, and reveal/use events
- support variable, config, and secret kinds
- implement scoped resolution: org, project, environment, app, workflow, server, target
- add encrypted secret storage
- add secret reveal with reason, audit, and optional step-up
- add config resolve endpoint for deployment/workflow targets
- add bulk env parser and dry-run validation

Exit criteria:

- effective config resolution is deterministic and tested
- secret values never appear in list/detail responses except one authorized reveal path
- secret use can happen without revealing plaintext to the user
- redaction tests cover API errors, logs, audit metadata, and job events

## Phase 4: Import/Export Primitive

- define logical export shapes for org, users, roles, groups, projects, config metadata, and future deploy resources
- add dry-run import validation model
- add ID remapping and conflict report model
- route large imports/exports through jobs

Exit criteria:

- exports use API shapes, not raw DB dumps
- token/session secrets are excluded
- encrypted secret export is explicitly separate from normal metadata export
- imports cannot grant permissions beyond actor authority

## Cross-Cutting Work

- tenant isolation tests with at least two orgs
- permission denial tests for every endpoint
- redaction tests for every log/error/audit/job path
- OpenTelemetry spans for audit, job, secret, config resolution paths
- cache invalidation on permission, config, and secret changes

## Research Sources

- `features/04-audit-activity.md`
- `shared/jobs-contract.md`
- `features/06-secrets-config-variables.md`
- `shared/import-export-contract.md`
- core-platform `features/03-secrets-variables-configs.md`
- core-platform `features/04-jobs-audit-events.md`
