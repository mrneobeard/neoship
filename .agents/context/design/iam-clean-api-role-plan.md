# IAM Clean API And Role Plan

## Outcome

IAM should expose a clean resource API and use code-owned standard role definitions instead of duplicating built-in role rows per organization.

## Standard Roles

The application owns these built-in role definitions by stable key:

- `owner`
- `admin`
- `editor`
- `reader`
- `auditor`

Built-in roles are global definitions. They are not copied per organization and org owners cannot edit them.

Role assignments are scoped. A built-in role assignment normally applies to one organization. Global role assignments are reserved for platform/global administration.

## Initial Meaning

- `owner`: full organization control, including owner management, org settings, delete, auth policy, identity providers, users, groups, roles, service accounts, and audit read.
- `admin`: IAM administration for the organization, excluding owner transfer, billing, and destructive organization deletion unless separately allowed.
- `editor`: product-resource mutation role. For IAM now, this should not grant IAM administration beyond self-service.
- `reader`: product-resource read role. For IAM now, this should grant minimal organization profile read.
- `auditor`: read audit/security/compliance data and IAM metadata, no mutations.

Project-level scopes are deferred until project resources exist. Permission grants should continue to use a scope kind so `project` can be added later without redesign.

## Custom Roles

Custom roles are database rows.

Initial scope:

- org-specific custom roles.

Later scope:

- multi-org custom roles only for global admins or users who own every target organization.
- global custom roles only for platform/global admins.

## Groups

Groups are always organization-specific.

Groups may receive built-in role assignments scoped to their organization and custom role assignments owned by their organization.

## API Direction

Avoid requiring `/orgs/{id}` on every route.

Use current organization from the session for routes that naturally target one organization:

- groups
- service accounts
- org-scoped role assignment
- invites
- auth policy

Require explicit `orgId` or `orgIds` only when an operation is cross-org or ambiguous.

Single-org hidden mode defaults to the default organization. Multi-org mode requires current organization context or an explicit org id for ambiguous operations.

Preferred API areas:

- `/api/v1/users`
- `/api/v1/sessions`
- `/api/v1/roles`
- `/api/v1/role-assignments`
- `/api/v1/groups`
- `/api/v1/service-accounts`
- `/api/v1/orgs`
- `/api/v1/me`
- `/api/v1/bootstrap` for first-install bootstrap

## Refactor Order

1. Add code-owned built-in role registry for the five standard roles.
2. Add scoped role assignment model that can reference built-in role keys or custom role ids.
3. Migrate existing org built-in role rows into assignments/custom roles where needed.
4. Keep groups org-specific.
5. Split route files by resource area.
6. Consolidate stores into user, session, organization, group, role, and service-account stores.
7. Move stateless helpers into `apps/api/ApiSvc/Lib` subfolders.
