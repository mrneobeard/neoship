# 02 First Deploy MVP Work Plan

## Goal

Ship the first useful deploy path: model a project/app, connect source or image, register a server, deploy Docker, expose it through a proxy/domain, and see logs/alerts.

## Prerequisites

- platform foundation work is complete enough for audit, jobs, and secrets
- tenant isolation and permission checks are enforced
- basic server-side generated OpenAPI exists before frontend implementation starts

## Phase 1: Projects, Environments, Applications

- add project CRUD
- add environment CRUD with production/staging/preview/custom types
- add application CRUD for web, worker, static, cron, database, service
- add dependency map metadata
- add default production/staging creation flow
- add archive instead of hard delete for objects with history

Exit criteria:

- project/env/app CRUD works with tenant isolation
- production environment defaults to approval-required
- dependency map rejects cross-tenant references

## Phase 2: Sources And Registries

- add source providers for GitHub, GitLab, and generic Git
- add repository sync and branch/ref selection
- add source webhook ingest with HMAC validation and dedupe
- add registry providers for Docker Hub, GHCR, GitLab Registry, generic OCI
- add Helm HTTP and Helm OCI metadata shape for later Kubernetes path
- store provider and registry credentials as secrets

Exit criteria:

- repository sync is paginated and rate limited
- webhook payloads are validated, deduped, redacted, and tenant-scoped
- registry credential test redacts tokens
- registry artifacts cache image/chart metadata without storing credentials

## Phase 3: Servers, SSH Credentials, Agents

- add server inventory
- add SSH credential vault with host key verification
- add server validation/probe job
- add Docker capability detection
- add approved server scripts and script runs
- add outbound agent registration placeholder or minimal agent path

Exit criteria:

- server probe runs as a job and writes audit
- SSH private keys and sudo passwords are encrypted
- host key policy defaults to strict or trust-on-first-use, never disabled
- script previews and logs are redacted

## Phase 4: Deployment Engine

- add deployment targets
- add deployment records and state machine
- add immutable source/artifact/config snapshots
- add deployment events/timeline
- add deployment trigger definitions and trigger events
- add create/list/detail/events/stream/cancel/rollback endpoints

Exit criteria:

- every deployment is job-backed
- every deployment records trigger, actor/service context, dedupe key, source, artifact, and config snapshot
- rollback creates a new deployment linked to the previous one
- deployment events stream over SSE

## Phase 5: Docker Deployments

- deploy existing image by tag or digest
- build Dockerfile from Git repository
- inject env/config/secrets safely
- support ports, named volumes, health checks, restart policy
- stream container logs
- implement recreate strategy first, blue/green when proxy is available
- rollback to previous image/config

Exit criteria:

- a simple container can be deployed to a Docker host
- health check failure marks deployment failed
- registry credentials decrypt only for pull/build
- env and secret values are redacted in logs and returned metadata

## Phase 6: Reverse Proxy, Domains, DNS, TLS

- add domains, proxies, certificates, DNS providers/zones/records
- support Traefik and Caddy first for Docker/Compose
- add Nginx template/validate/reload path
- add Cloudflare first, then Route53/Azure DNS as planned
- add TLS status and renewal tracking

Exit criteria:

- app can be exposed through a managed domain with TLS status
- proxy config is validated before reload
- certificate expiry and DNS drift can raise alerts
- cross-tenant domain/route collisions are prevented

## Phase 7: Observability, Logs, Alerts

- add deployment and job log query
- add runtime log query for Docker/Compose/Kubernetes later
- add server metrics basics
- add alert rules and alert instances
- add notification channels for email, Slack, Discord, and webhook

Exit criteria:

- failed deploy opens an alert
- logs are paged, searchable by bounded filters, and redacted
- notification tests redact tokens and secrets
- deployment detail links logs, events, job, target, artifact, and rollback point

## Research Sources

- `features/05-projects-environments.md`
- `features/07-sources-registries.md`
- `features/08-servers-agents.md`
- `features/09-deployment-engine.md`
- `features/10-docker-deployments.md`
- `features/13-reverse-proxy-domains.md`
- `features/15-observability-alerts.md`
- `05-final-build-spec.md`
