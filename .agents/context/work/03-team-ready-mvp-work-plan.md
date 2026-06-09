# 03 Team Ready MVP Work Plan

## Goal

Make the product useful for real teams and common self-hosting workflows after the first Docker deploy path exists.

## Prerequisites

- first deploy MVP is usable end-to-end
- job, audit, secret, deployment, and alert foundations are active
- proxy/domain path exists for public app access

## Phase 1: Docker Compose Deployments And Previews

- support Compose file from repo or editor
- support multiple compose files and profiles
- render `.env` from NeoShip config
- validate with `docker compose config` equivalent
- isolate project names
- stream logs per service
- add pull request preview deployments with TTL cleanup
- rollback to previous rendered bundle

Exit criteria:

- multi-service Compose fixture deploys and rolls back
- invalid Compose errors point to fields
- preview deployments do not inherit production secrets by default
- rendered bundle metadata is redacted

## Phase 2: Backups And Restore

- add backup destinations with S3-compatible first
- add backup resources and plans
- add backup runs and restore runs
- support Postgres, MySQL/MariaDB, SQLite, SQL Server, ClickHouse, and volume backup strategies as scoped
- add restore dry-run and overwrite approval/confirmation
- show backup freshness on app/deploy screens

Exit criteria:

- backup runs are jobs with progress, checksum, artifact URI, and retention cleanup
- restore overwrite is gated as critical risk
- backup logs redact DB URLs and passwords
- stale/failed backups can raise alerts

## Phase 3: Integrations And Webhooks

- add integration catalog for GitHub, GitLab, Slack, Discord, email, generic webhook
- add outbound webhook subscriptions and delivery logs
- add inbound webhook endpoints for deployments and workflows
- add HMAC signing, replay protection, retries, redelivery, and SSRF controls

Exit criteria:

- outbound webhooks are signed and retried with backoff
- inbound webhooks reject invalid signatures and replayed payloads
- notification payloads redact secrets
- webhook delivery logs are queryable and tenant-scoped

## Phase 4: Workflows, Schedules, Runbooks

- add workflows and immutable workflow versions
- add workflow runs backed by jobs
- add scheduled, manual, signed HTTP/API, source, registry, deployment, and alert triggers
- add approved runbooks with typed inputs
- add deploy, rollback, backup, notify, HTTP, Docker structured actions
- add shell/script steps through approved server script path

Exit criteria:

- workflow runs are audited and job-backed
- production runbooks require approved version
- HTTP/API triggers are signed, replay-protected, rate limited, and schema validated
- secret use requires `secrets.use`

## Phase 5: Import And Export

- add org/project/app/config/template metadata exports
- add CSV/JSONL audit export
- add dry-run import validation and reports
- add large import/export job path
- add encrypted portable secret export later behind explicit mode

Exit criteria:

- exports are tenant-scoped and redacted
- imports validate references, slugs, names, scopes, permissions, and conflicts before applying
- imports cannot escalate privileges
- export/download URLs are short-lived and actor-bound

## Phase 6: Service Template Catalog

- add curated service templates for common apps and databases
- add template inputs and validation
- add generated config/deploy target preview
- add one-click or guided create flow from template

Exit criteria:

- template import has dry-run preview
- generated resources follow tenant and permission rules
- secrets are references, not plaintext template values

## Phase 7: Billing And Licensing

- add license/capability checks
- add plan gates and usage limits
- add billing hooks for hosted control plane later
- add enterprise gates for SSO, custom RBAC, audit export, database-per-org, whitelabeling

Exit criteria:

- feature gates are server-side and audited
- OSS and enterprise modules can coexist without schema rewrites
- plan changes invalidate capability cache

## Research Sources

- `features/11-compose-deployments.md`
- `features/14-backups-restore.md`
- `features/18-integrations-webhooks.md`
- `features/16-workflows-runbooks.md`
- `shared/import-export-contract.md`
- `features/21-service-template-catalog.md`
- `features/19-billing-licensing.md`
