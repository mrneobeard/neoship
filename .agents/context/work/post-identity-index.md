# Post-Identity Work Index

## Purpose

Track the feature work that follows identity, tenants, and authorization.

Use `index.md` as the top-level work index. This document owns the detailed post-identity sequence.

This ordering is pulled from the local work docs and the research docs under:

- `~/repos/neo/research/docs/apps/neocloud/book/01-feature-inventory.md`
- `~/repos/neo/research/docs/apps/neocloud/book/05-final-build-spec.md`
- `~/repos/neo/research/docs/apps/neocloud/book/features/`
- `~/repos/neo/research/docs/apps/neocloud/book/shared/`
- `~/repos/neo/research/docs/apps/core-platform/book/`

## Status Key

- Done: implemented and tested
- In Progress: active implementation started
- Blocked: intentionally waiting on prerequisite work
- Not Started: planned but not started

## Current Prerequisites

| Order | Work | Status | Notes |
| --- | --- | --- | --- |
| P-01 | Identity and sessions | In Progress | See `identity-api-work-plan.md` and `identity-api-tickets.md`. |
| P-02 | Tenants and organizations | In Progress | Current model has org scope; complete tenant APIs before deploy features. |
| P-03 | Authorization, roles, groups, permissions | In Progress | Must be reliable before privileged operations. |
| P-04 | API/query contracts | Done | See `api-contract.md` and `querying.md`. |
| P-05 | Multi-database/AppHost base | In Progress | See `multi-db-apphost-work-plan.md`. |

## Ordered Feature Work

| Order | Feature | Status | Work Doc |
| --- | --- | --- | --- |
| 01 | Audit and activity | Blocked | `01-platform-foundation-after-identity-work-plan.md` |
| 02 | Jobs and long-running operations | Blocked | `01-platform-foundation-after-identity-work-plan.md` |
| 03 | Secrets, configs, and variables | Blocked | `01-platform-foundation-after-identity-work-plan.md` |
| 04 | Projects, environments, applications | Not Started | `02-first-deploy-mvp-work-plan.md` |
| 05 | Sources and registries | Not Started | `02-first-deploy-mvp-work-plan.md` |
| 06 | Servers, SSH credentials, and agents | Not Started | `02-first-deploy-mvp-work-plan.md` |
| 07 | Deployment engine | Not Started | `02-first-deploy-mvp-work-plan.md` |
| 08 | Docker deployments | Not Started | `02-first-deploy-mvp-work-plan.md` |
| 09 | Reverse proxy, domains, DNS, TLS | Not Started | `02-first-deploy-mvp-work-plan.md` |
| 10 | Observability, logs, alerts | Not Started | `02-first-deploy-mvp-work-plan.md` |
| 11 | Docker Compose deployments and previews | Not Started | `03-team-ready-mvp-work-plan.md` |
| 12 | Backups and restore | Not Started | `03-team-ready-mvp-work-plan.md` |
| 13 | Integrations and webhooks | Not Started | `03-team-ready-mvp-work-plan.md` |
| 14 | Workflows, schedules, and runbooks | Not Started | `03-team-ready-mvp-work-plan.md` |
| 15 | Import/export | Not Started | `03-team-ready-mvp-work-plan.md` |
| 16 | Service template catalog | Not Started | `03-team-ready-mvp-work-plan.md` |
| 17 | Billing and licensing | Not Started | `03-team-ready-mvp-work-plan.md` |
| 18 | Kubernetes deployments | Not Started | `04-enterprise-expansion-work-plan.md` |
| 19 | Enterprise evidence, admin ops, read-only AI | Not Started | `04-enterprise-expansion-work-plan.md` |
| 20 | Later platform targets | Not Started | `05-later-platform-targets-work-plan.md` |

## Dependency Rules

- identity -> tenancy -> authorization -> audit
- authorization + audit -> secrets
- jobs -> deploys, backups, workflows, imports, exports
- secrets -> sources, registries, servers, deployments, backups, integrations
- projects -> applications -> deployment targets -> deployments
- sources + registries + servers -> Docker deployment
- deployment engine -> Docker, Compose, Kubernetes, rollback, triggers
- proxy/domains -> blue-green cutover, public app access, TLS health
- observability -> alerts, diagnostics, support, AI read-only analysis
- workflows -> deploy hooks, scheduled backups, runbooks
- billing/licensing -> limits, plans, enterprise gates

## Product-Wide Acceptance Rules

- every tenant-owned object has org isolation in API, DB queries, jobs, logs, audit, and exports
- every mutation checks permission and writes an audit event
- every long operation returns a job and exposes progress
- every import supports dry-run validation
- every export is tenant-scoped and redacts secrets by default
- every production-risk action shows impact and rollback or recovery path
- every list endpoint follows `querying.md`
- every response follows `api-contract.md`
- every secret, credential, token, and private key is encrypted or hashed and never logged
