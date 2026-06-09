# Work Index

## Purpose

Track active and planned work in one ordered place.

Start here before opening individual work plans.

## Status Key

- Done: implemented and tested
- In Progress: active implementation started
- Blocked: waiting on prerequisite work
- Not Started: planned but not started

## Current Work

| Order | Work | Status | Plan | Tickets |
| --- | --- | --- | --- | --- |
| 01 | Identity API | In Progress | `identity-api-work-plan.md` | `identity-api-tickets.md` |
| 02 | Multi-database and AppHost | In Progress | `multi-db-apphost-work-plan.md` | `multi-db-apphost-tickets.md` |
| 03 | Post-identity platform features | Blocked | `post-identity-index.md` | `post-identity-feature-tickets.md` |

## Immediate Identity Sequence

Finish these before starting deployment-platform work:

1. MFA and passkeys
2. remaining role/group/custom-role endpoints and tests
3. identity provider OIDC foundation
4. auth middleware for user API keys, service account API keys, and JWT exchange
5. cache abstraction and permission/session invalidation
6. API result envelopes and query helpers wired into endpoints

## Post-Identity Sequence

Use `post-identity-index.md` for the canonical feature order.

High-level sequence:

1. platform foundation: audit, jobs, secrets, import/export primitives
2. first deploy MVP: projects, sources, servers, deployment engine, Docker, proxy, logs/alerts
3. team-ready MVP: Compose, backups, integrations, workflows, templates, billing/licensing
4. enterprise expansion: Kubernetes, evidence, admin ops, read-only AI
5. later targets: common cloud compute, Proxmox/KVM, Aspire/Nix, marketplace

## Cross-Cutting Rules

- response envelopes follow `../contracts/api-contract.md`
- list/query behavior follows `../contracts/querying.md`
- auth/security behavior follows `../contracts/security.md`
- cache defaults follow `../contracts/caching.md`
- telemetry/logging follows `../contracts/otel.md`, `../contracts/telemetry.md`, and `../contracts/logging.md`
- every code change requires relevant build/test/format verification before finishing

## Done So Far

- API/query contract docs exist
- identity API foundation has active tickets and many completed slices
- provider-specific migrations and multi-db/AppHost work have active plans
- post-identity feature plans and tickets exist

## Not Done Yet

- identity module is not complete
- endpoint response envelopes are not uniformly implemented
- query helper implementation is not started
- audit/job/secret foundations for post-identity features are not started
- deploy-platform feature work is not started
