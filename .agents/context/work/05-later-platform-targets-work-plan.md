# 05 Later Platform Targets Work Plan

## Goal

Track expansion work after the MVP and enterprise base prove value.

These features should not block identity, deploy MVP, team-ready MVP, or Kubernetes/enterprise work.

## Phase 1: Common Cloud Compute Targets

- AWS ECS/Fargate/App Runner/Lambda jobs
- Azure App Service and Container Apps
- Google Cloud Run and GKE Autopilot helpers
- provider credential storage as secrets
- provider capability probes and cost/risk previews

Exit criteria:

- at least one cloud target can deploy from the generic deployment engine
- provider errors map to stable API error codes
- credentials and generated plans are redacted

## Phase 2: Proxmox And KVM/libvirt

- add provider inventory for Proxmox and libvirt
- add VM/template metadata sync
- add provisioning job model
- add network/storage plan preview
- add rollback/delete safety model

Exit criteria:

- provisioning is job-backed and audited
- dangerous infrastructure mutations require confirmation or approval
- provider state drift is detectable

## Phase 3: Aspire, Nix, NixOS, Flakes

- add deploy target for `.NET Aspire` app models where useful
- add Nix/NixOS/flake deploy flow for reproducible hosts
- add generated plan preview before apply
- integrate with server/agent and workflow primitives

Exit criteria:

- generated plans are reviewable and reproducible
- apply is job-backed and rollback-aware where supported
- secrets remain external references

## Phase 4: Advanced Workflows And Marketplace

- expand workflow engine beyond deploy hooks and scheduled jobs
- add reusable workflow packages
- add marketplace/template publishing and trust model
- add signed templates and provenance later

Exit criteria:

- templates cannot escalate permissions
- imported packages run through dry-run validation
- marketplace content is isolated from tenant secrets

## Delayed Explicitly

- hosted compute owned by NeoShip
- full Kubernetes cluster management beyond app deploy operations
- arbitrary IaC execution without typed plans and policy gates
- AI-assisted mutations without strong policy, confirmation, and audit

## Research Sources

- `features/24-common-cloud-compute.md`
- `features/23-virtualization-proxmox-kvm.md`
- `features/25-aspire-nix-developer-platforms.md`
- `01-feature-inventory.md`
- `03-road-to-1.0-expansion.md`
