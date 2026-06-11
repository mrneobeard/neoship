# Identity API Contract

## Purpose

Define the public API rules for identity service mode and networked module APIs.

The contract must be predictable for clients and implementable by servers in C#, Go, Rust, TypeScript, Java, or other languages. Public shapes must not depend on framework-specific result types.

This imports and specializes useful context from the research docs:

- `~/repos/neo/research/docs/apps/core-platform/book/shared/api-contract.md`
- `~/repos/neo/research/docs/apps/core-platform/book/shared/data-contract.md`
- `~/repos/neo/research/docs/apps/core-platform/book/shared/security-contract.md`
- `~/repos/neo/research/docs/apps/core-platform/book/features/01-identity-sessions-passkeys.md`
- `~/repos/neo/research/docs/apps/core-platform/book/features/02-tenancy-memberships-rbac.md`
- `~/repos/neo/research/docs/apps/core-platform/book/features/04-jobs-audit-events.md`
- `~/repos/neo/research/docs/apps/neocloud/book/shared/api-contract.md`
- `~/repos/neo/research/docs/apps/neocloud/book/shared/data-contract.md`
- `~/repos/neo/research/docs/apps/neocloud/book/shared/security-contract.md`
- `~/repos/neo/research/docs/apps/neocloud/book/features/01-identity-access.md`
- `~/repos/neo/research/docs/apps/neocloud/book/features/02-tenancy-orgs.md`
- `~/repos/neo/research/docs/apps/neocloud/book/features/03-rbac-groups.md`
- `~/repos/neo/research/docs/apps/neocloud/book/features/04-audit-activity.md`

## Base Rules

- REST/JSON for control-plane operations
- `/api/v1` prefix
- UUIDv7 resource ids
- JSON property names use `camelCase`
- SQL names remain internal and use `snake_case`
- timestamps use RFC 3339 UTC strings
- request and response payloads are language-neutral JSON objects
- OpenAPI 3.1 is generated and published from source-controlled endpoint definitions
- embedded in-process module APIs should keep equivalent semantics but do not need network envelopes

## Routing Rules

- auth routes use `/api/v1/auth/...`
- current actor routes use `/api/v1/me/...`
- current-organization routes infer org context from the actor and use clean resource nouns such as `/api/v1/roles`, `/api/v1/groups`, and `/api/v1/service-accounts`; explicit org-admin routes may use `/api/v1/org/{orgId}/...` when the org id is part of the resource identity
- route nouns use public product terms, not table names
- resource ids in payloads are UUIDv7 unless a resource explicitly documents another opaque id format
- slugs are for URLs and human selection
- current-organization routes never trust an org id in the request body
- tenant context is resolved before resource access
- every request resolves actor context and tenant context before feature handlers run

## Authentication Rules

Supported request auth:

- session cookie
- `Authorization: Bearer <user-api-key>`
- `Authorization: Bearer <service-account-api-key>`
- `Authorization: Bearer <short-lived-encrypted-jwt>`
- optional `Authorization: Basic ...` only over TLS when explicitly enabled

Rules:

- default deny
- permissions are evaluated from current DB/cache state
- product modules register permissions
- service accounts use scoped ceilings
- step-up auth is supported for high-risk operations
- JWTs must not be treated as authoritative full role blobs

## Success Envelope

All successful endpoints return a top-level JSON object.

Single resource:

```json
{
  "data": {
    "id": "018f0000-0000-7000-8000-000000000001",
    "type": "role",
    "attributes": {
      "name": "admin"
    }
  },
  "meta": {
    "requestId": "req_...",
    "traceId": "...",
    "serverTime": "2026-05-28T12:00:00Z"
  }
}
```

Collection:

```json
{
  "data": [
    {
      "id": "018f0000-0000-7000-8000-000000000001",
      "type": "role",
      "attributes": {
        "name": "admin"
      }
    }
  ],
  "pagination": {
    "limit": 50,
    "nextCursor": "opaque-cursor",
    "previousCursor": null,
    "hasMore": true
  },
  "meta": {
    "requestId": "req_...",
    "traceId": "...",
    "serverTime": "2026-05-28T12:00:00Z"
  }
}
```

Mutation with no resource body:

```json
{
  "data": null,
  "meta": {
    "requestId": "req_...",
    "traceId": "...",
    "serverTime": "2026-05-28T12:00:00Z"
  }
}
```

## Metadata Object

`meta` carries response-level metadata.

Fields:

- `requestId`: server request id for support and audit correlation
- `traceId`: distributed trace id when available
- `serverTime`: RFC 3339 UTC response timestamp
- `query`: optional normalized query echo for collection endpoints

Rules:

- `requestId` is included in every network response envelope
- `traceId` is included when available
- `serverTime` is included for client clock comparison and debugging
- `meta` never includes secrets, bearer tokens, cookies, or raw credentials

## Resource Object

Resources use the same shape across endpoints.

Fields:

- `id`: stable resource id when the object has one
- `type`: stable public resource type, not a CLR type name
- `attributes`: scalar properties and small value objects
- `relationships`: ids or compact relationship summaries
- `links`: optional resource links when useful

Rules:

- list responses return compact resource objects by default
- detail responses return fuller resource objects but still omit secrets
- expensive child collections require `expand`
- secrets, token digests, hashes, ciphertext, and encrypted blobs never appear
- internal database column names never appear
- tenant-specific labels may be adapted in UI, but API resource types stay stable

Example relationship summary:

```json
{
  "relationships": {
    "org": {
      "id": "018f0000-0000-7000-8000-000000000002",
      "type": "org"
    }
  },
  "meta": {
    "requestId": "req_...",
    "serverTime": "2026-05-28T12:00:00Z"
  }
}
```

## Error Envelope

Failures return a top-level `error` object.

```json
{
  "error": {
    "code": "permission_denied",
    "message": "Permission denied.",
    "details": {
      "permission": "org.roles.write"
    }
  },
  "meta": {
    "requestId": "req_...",
    "traceId": "...",
    "serverTime": "2026-05-28T12:00:00Z"
  }
}
```

Rules:

- `code` is stable and machine-readable
- `message` is safe for humans
- `details` is optional and safe to expose
- validation errors include field paths under `details.fields`
- stack traces and raw exception messages never appear
- sensitive values are redacted before entering errors, logs, audit events, exports, or support bundles

Validation example:

```json
{
  "error": {
    "code": "validation_failed",
    "message": "Validation failed.",
    "details": {
      "fields": {
        "email": ["Invalid email address."],
        "name": ["Must be 120 characters or fewer."]
      }
    }
  }
}
```

## Error Codes

Use shared codes first:

- `unauthenticated`
- `permission_denied`
- `tenant_not_found`
- `not_found`
- `conflict`
- `validation_failed`
- `rate_limited`

Auth-specific detail codes may appear under `error.details.code`:

- `invalid_credentials`
- `password_reset_token_invalid`
- `email_verification_token_invalid`
- `mfa_required`
- `step_up_required`
- `api_key_revoked`
- `api_key_expired`

## HTTP Status Rules

- `200 OK` for successful reads and idempotent actions with a body
- `201 Created` for creates with a `Location` header
- `202 Accepted` only for async work that is not complete
- `204 No Content` is allowed for raw HTTP compatibility, but identity APIs should prefer `{ "data": null }` when clients need correlation metadata
- `400 Bad Request` for malformed requests
- `401 Unauthorized` for missing or invalid authentication
- `403 Forbidden` for authenticated users without permission
- `404 Not Found` for missing resources or hidden resources
- `409 Conflict` for uniqueness or version conflicts
- `422 Unprocessable Entity` for semantic validation failures
- `429 Too Many Requests` for rate limits

## Pagination Envelope

Collection endpoints use a `pagination` object when paged.

Fields:

- `limit`: actual item limit used
- `nextCursor`: cursor for the next page, or `null`
- `previousCursor`: optional cursor for the previous page, or `null`
- `hasMore`: whether another page exists

Rules:

- cursors are opaque
- raw database offsets are not exposed
- cursor internals are not documented as public contract
- mutable lists use cursor pagination
- default `limit` is 50
- maximum `limit` is 200 unless a resource explicitly lowers it

## Query Echo

Endpoints may include a normalized query echo in `meta.query` for debugging and client predictability.

```json
{
  "meta": {
    "query": {
      "filter": { "status": "active" },
      "sort": ["name"],
      "expand": ["claims"],
      "limit": 50,
      "cursor": null
    }
  }
}
```

## Batch Result Envelope

Batch endpoints return per-item results.

```json
{
  "data": [
    {
      "index": 0,
      "status": 200,
      "data": { "id": "018f0000-0000-7000-8000-000000000001", "type": "role" }
    },
    {
      "index": 1,
      "status": 409,
      "error": { "code": "conflict", "message": "Already exists." }
    }
  ],
  "meta": {
    "requestId": "req_...",
    "traceId": "..."
  }
}
```

Rules:

- batch result order matches request item order
- each item has `index` and `status`
- item results contain either `data` or `error`
- batch-level errors are only for invalid batch requests, not per-item failures

## Idempotency

- mutating create-style endpoints should accept `Idempotency-Key` when retries are likely
- idempotency keys are scoped to actor, tenant, route, method, and normalized request body
- repeated requests with the same key return the same result or a clear conflict
- same key with a different request body returns `409 conflict`
- idempotency records are retained for at least 24 hours unless an endpoint documents a stricter rule
- idempotency records must not store secrets in plaintext

## Optimistic Concurrency

- mutable resources expose `version`
- updates include `ifVersion` in the JSON body or `If-Match` in headers
- version mismatches return `409 conflict`
- conflict details should include latest safe version metadata when useful

Example:

```json
{
  "name": "admin",
  "ifVersion": 4
}
```

## Validation

- validate syntactic schema at the boundary
- validate business rules in store/domain code
- validate tenant ownership on every referenced id
- normalize strings before uniqueness checks
- reject unknown enum values
- reject payloads over endpoint size limits
- reject unknown filters, sorts, expands, and unsupported query params with `validation_failed`

## Async Operations

- normal API requests should target completion within 30 seconds
- operations that may exceed 4.5 minutes must return `202 Accepted`
- async responses return a job resource or job relationship in `data`
- job logs are redacted before persistence
- cancellation and retry semantics must be documented per endpoint

Example:

```json
{
  "data": {
    "id": "018f0000-0000-7000-8000-000000000010",
    "type": "job",
    "attributes": {
      "status": "queued"
    },
    "links": {
      "self": "/api/v1/jobs/018f0000-0000-7000-8000-000000000010"
    }
  },
  "meta": {
    "requestId": "req_...",
    "serverTime": "2026-05-28T12:00:00Z"
  }
}
```

## Audit Semantics

Audit is required for:

- login and security changes
- member, role, group, permission, and policy changes
- service account and API key changes
- secret create, update, use, reveal, and delete if secrets are added to this module
- license or capability changes if licensing is added to this module
- high-risk product actions registered by product modules

Audit events are append-only and include actor, tenant, target, action, IP address, user agent, risk level, and request id when available.

## Rate Limit Headers

When rate limited, return `429 Too Many Requests` with the error envelope.

Rate limit dimensions:

- actor id
- org id
- IP address
- endpoint category
- token id for API keys

Recommended headers:

- `Retry-After`
- `RateLimit-Limit`
- `RateLimit-Remaining`
- `RateLimit-Reset`

Legacy `X-RateLimit-*` headers may be emitted for client compatibility but are not the canonical contract.

Default limits:

| Category | Limit | Notes |
| --- | --- | --- |
| Auth login | `10/min/IP`, `20/hour/user` | Progressive delay after failures. |
| General API | `600/min/actor` | Higher for trusted internal workers. |
| Search/list filters | `120/min/actor` | Protects DB. |
| Token creation | Endpoint-specific | Strict per actor and org. |
| Secret reveal | `20/hour/actor` | Only if secrets are added to this module. |

## Compatibility Rules

- additive fields are allowed
- removing or renaming public fields is breaking
- enum-like strings must be documented
- unknown fields should be ignored by clients
- clients must not depend on object property order
- cursors are opaque and may change format
- numeric ids are not used in public identity API routes or payloads
- public contracts describe JSON, not C# classes

## Export Rules

- exports use logical API shapes, not raw database dumps
- exported identity data includes metadata, memberships, roles, groups, and token metadata only
- token plaintext and session secrets are never exported
- export endpoints that may process more than 100 resources should return async jobs
