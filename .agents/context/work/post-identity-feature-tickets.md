# Post-Identity Feature Tickets

## Ticket Status Key

- Done: implemented and tested
- In Progress: active implementation started
- Blocked: waiting on prerequisite work
- Not Started: planned but not started

## Foundation Tickets

### PI-001 Audit Event Foundation

Status: Blocked

Scope:

- append-only audit model, writer, list/detail API, filters, redaction

Acceptance:

- all privileged mutations emit audit events
- cross-tenant access is denied
- audit list follows `querying.md`

### PI-002 Job Runner Foundation

Status: Blocked

Scope:

- job model, job events, leases, retries, cancellation, SSE stream

Acceptance:

- long operations return `202 Accepted` with job resource
- worker lease behavior is tested on supported DBs
- job logs are redacted

### PI-003 Secrets And Config Foundation

Status: Blocked

Scope:

- variables, configs, secrets, versions, scoped resolution, reveal/use audit

Acceptance:

- config resolution is deterministic
- secret reveal is one authorized path only
- redaction tests cover API, logs, audit, and jobs

### PI-004 Import Export Primitive

Status: Not Started

Scope:

- logical export shapes, dry-run import validation, ID remap, conflict report

Acceptance:

- exports are tenant-scoped and redacted
- imports cannot escalate permissions

## First Deploy MVP Tickets

### PI-005 Projects Environments Applications

Status: Not Started

Scope:

- project, environment, application CRUD and dependency map

Acceptance:

- CRUD is tenant-isolated
- production defaults require approval

### PI-006 Sources And Registries

Status: Not Started

Scope:

- GitHub, GitLab, generic Git, Docker/OCI registries, source events

Acceptance:

- provider tokens are secrets
- webhook HMAC and dedupe are tested

### PI-007 Servers SSH Agents

Status: Not Started

Scope:

- server inventory, SSH credentials, host keys, probes, script runs, agent placeholder

Acceptance:

- host key verification is enforced by default
- credential use is audited

### PI-008 Deployment Engine

Status: Not Started

Scope:

- deployment targets, deployments, triggers, snapshots, events, cancel, rollback

Acceptance:

- deployments are job-backed
- rollback creates a linked deployment

### PI-009 Docker Deployments

Status: Not Started

Scope:

- image deploy, Dockerfile build, env/secret injection, logs, health, rollback

Acceptance:

- simple container deploys to Docker test host
- health failure marks deployment failed

### PI-010 Reverse Proxy Domains TLS

Status: Not Started

Scope:

- domains, proxies, DNS providers/zones/records, certificates, renewals

Acceptance:

- managed domain can expose app with TLS status
- route collisions are prevented

### PI-011 Observability Alerts Notifications

Status: Not Started

Scope:

- logs, metrics basics, alerts, notification channels

Acceptance:

- deploy failure opens alert
- logs are paged and redacted

## Team Ready MVP Tickets

### PI-012 Docker Compose Deployments

Status: Not Started

Scope:

- Compose files, profiles, rendered env, validation, previews, rollback

Acceptance:

- multi-service fixture deploys and rolls back
- previews do not inherit production secrets by default

### PI-013 Backups Restore

Status: Not Started

Scope:

- destinations, resources, plans, runs, restore dry-run, retention

Acceptance:

- backup and restore are job-backed
- overwrite restore requires approval/confirmation

### PI-014 Integrations Webhooks

Status: Not Started

Scope:

- integrations, outbound webhooks, delivery logs, inbound endpoints

Acceptance:

- outbound payloads are signed
- inbound replay is rejected

### PI-015 Workflows Runbooks

Status: Not Started

Scope:

- workflows, versions, runs, schedules, runbooks, typed inputs

Acceptance:

- runs are job-backed and audited
- production runbooks require approval

### PI-016 Import Export Product APIs

Status: Not Started

Scope:

- project/app/config/audit exports and import reports

Acceptance:

- large imports/exports run as jobs
- exports redact secrets by default

### PI-017 Service Template Catalog

Status: Not Started

Scope:

- curated templates, inputs, generated plan preview, guided create

Acceptance:

- template apply uses dry-run validation
- generated secrets are references only

### PI-018 Billing Licensing

Status: Not Started

Scope:

- license/capability checks, plans, limits, enterprise gates

Acceptance:

- feature gates are server-side and audited
- capability cache invalidates on plan change

## Enterprise And Expansion Tickets

### PI-019 Kubernetes Deployments

Status: Not Started

Scope:

- clusters, manifests, rollout status, logs/events, Helm, cert-manager

Acceptance:

- generated manifest deploys to test cluster
- rollout/rollback visible in deployment timeline

### PI-020 Enterprise Controls And Evidence

Status: Not Started

Scope:

- custom RBAC hardening, approvals, audit export, SOC 2 evidence packages

Acceptance:

- evidence exports are tenant-scoped and redacted
- custom roles cannot escalate privileges

### PI-021 Read-Only AI Diagnostics

Status: Not Started

Scope:

- read redacted diagnostics, logs, events, docs; no mutation tools initially

Acceptance:

- AI cannot access plaintext secrets
- untrusted content is treated as data

### PI-022 Admin Ops

Status: Not Started

Scope:

- health dashboard, support bundles, migrations, worker health, update/license views

Acceptance:

- support bundles are redacted
- admin ops are audited

### PI-023 Later Platform Targets

Status: Not Started

Scope:

- common cloud compute, Proxmox/KVM, Aspire/Nix, advanced workflows, marketplace

Acceptance:

- each target is job-backed, audited, redacted, and uses typed plans
