# 04 Enterprise Expansion Work Plan

## Goal

Add Kubernetes and enterprise-grade controls after the Docker/Compose team MVP is stable.

## Prerequisites

- deployment engine supports generic targets and immutable snapshots
- secrets, jobs, audit, alerts, backup, proxy, and workflows are active
- enterprise module boundary and capability gates are established

## Phase 1: Kubernetes Deployments

- add cluster inventory and capability probe
- add namespace-per-org/project/environment strategy
- generate app manifests for Deployment, Service, Ingress/Gateway, ConfigMap, Secret refs, PVC, HPA later
- integrate cert-manager status
- support logs, events, rollout status, pod describe, restarts, image pull errors
- support Helm chart deploy path from registry artifacts

Exit criteria:

- generated app manifest deploys to a test cluster
- rollout/rollback status is visible through deployment timeline
- secrets remain references or Kubernetes secrets created through controlled flow
- cluster capability cache refreshes before apply

## Phase 2: Enterprise Access Controls

- finish OIDC/SAML/SCIM enterprise hardening as needed
- support custom RBAC and scoped role assignments where not already complete
- add approval policy and separation-of-duty checks
- add audit export capability gate
- add database-per-org migration job plan

Exit criteria:

- custom roles cannot grant permissions actor lacks unless owner policy allows
- production approvals cannot be self-approved when policy disallows
- audit export is permissioned, redacted, and plan-gated

## Phase 3: SOC 2 Evidence Source

- expose evidence metadata for deployments, backups, secrets, audit, access changes, incidents, and policy checks
- add scoped evidence export packages
- add retention and legal-hold hooks later
- avoid leaking secrets or unrelated tenant data

Exit criteria:

- evidence export can prove control activity without plaintext secrets
- every included record ties back to audit/request/job ids where available
- exports are tenant-scoped and permissioned

## Phase 4: Read-Only AI Assistant

- allow AI to read redacted diagnostic output, logs, deployment timelines, alerts, and docs
- add strict tool allowlists and no-secret policy
- add prompt-injection handling for logs, repo files, webhook payloads, and app output
- begin read-only diagnosis before mutations

Exit criteria:

- AI cannot read plaintext secrets by default
- AI treats untrusted content as data, not instructions
- AI recommendations link to diagnostic evidence

## Phase 5: Admin And Ops

- add self-hosted health dashboard
- add support bundle generation with redaction
- add backup/restore and migration operation views
- add license, update, telemetry, and worker health screens

Exit criteria:

- support bundle excludes secrets and sensitive tokens
- admin ops are audited
- self-hosted operator can diagnose broken jobs, DB connectivity, workers, and storage

## Research Sources

- `features/12-kubernetes-deployments.md`
- `features/22-soc2-evidence-source.md`
- `features/17-ai-assistant.md`
- `features/20-admin-ops.md`
- `shared/security-contract.md`
- `shared/observability-contract.md`
