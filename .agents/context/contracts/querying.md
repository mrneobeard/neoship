# Identity Query Contract

## Purpose

Define one horizontal query contract for list, detail, search, expand, and batch APIs.

This contract must line up with `api-contract.md`. Query behavior must be predictable and implementable in any language without LINQ, OData, GraphQL, RSQL, or framework-specific query objects.

## Default Shape

- list endpoints return compact resource objects by default
- detail endpoints return fuller resource objects but still omit secrets
- related data is opt-in through `expand`
- server-side filtering, sorting, and paging are required for any collection that can grow
- all collection responses use `data`, `pagination`, and `meta` from `api-contract.md`

Example:

```json
{
  "data": [],
  "pagination": {
    "limit": 50,
    "nextCursor": null,
    "previousCursor": null,
    "hasMore": false
  },
  "meta": {
    "requestId": "req_...",
    "traceId": "...",
    "serverTime": "2026-05-28T12:00:00Z"
  }
}
```

## Query Parameters

Standard list parameters:

| Parameter | Meaning |
| --- | --- |
| `filter[name]` | Allowlisted filter by stable public field name. |
| `filter[createdAt.gte]` | Allowlisted operator filter. |
| `sort` | Comma-separated allowlisted sort fields. Prefix field with `-` for descending. |
| `limit` | Page size. Default `50`, maximum `200` unless endpoint lowers it. |
| `cursor` | Opaque cursor returned by a previous response. |
| `expand` | Comma-separated allowlisted related data. |
| `q` | Endpoint-defined search string. Must be rate limited. |

Rules:

- unknown query parameters return `validation_failed`
- unknown filters return `validation_failed`
- unknown sort fields return `validation_failed`
- unknown expands return `validation_failed`
- unsupported operators return `validation_failed`
- query parameter names are case-sensitive
- public query names are stable domain names, not database column names
- endpoints document their filter, sort, expand, and search allowlists

## Filtering

Use `filter[...]` for structured filters.

Equality:

```text
GET /api/v1/orgs/acme/roles?filter[name]=admin
```

Multiple exact values:

```text
GET /api/v1/orgs/acme/memberships?filter[status]=active,invited
```

Range operators:

```text
GET /api/v1/orgs/acme/audit-events?filter[createdAt.gte]=2026-01-01T00:00:00Z&filter[createdAt.lt]=2026-02-01T00:00:00Z
```

Supported operators:

| Operator | Meaning |
| --- | --- |
| no operator | Equal to one value or any comma-separated value. |
| `.ne` | Not equal. Use sparingly. |
| `.gt` | Greater than. |
| `.gte` | Greater than or equal. |
| `.lt` | Less than. |
| `.lte` | Less than or equal. |
| `.prefix` | Prefix match on indexed normalized field. |

Rules:

- expose only filters backed by indexes or cheap bounded evaluation
- do not expose arbitrary expressions
- do not expose raw contains filters on large text fields by default
- do not expose cross-tenant filters that can bypass route tenant context
- list filters must include tenant scope internally for tenant resources
- every referenced id is validated within the resolved tenant

## Search

Use `q` only for endpoint-defined text search.

Example:

```text
GET /api/v1/orgs/acme/groups?q=deploy
```

Rules:

- `q` is not a general SQL or expression language
- minimum search length is 2 characters unless endpoint documents another value
- maximum search length is 120 characters unless endpoint documents another value
- search uses normalized shadow fields where practical
- search/list filters use stricter rate limits than basic reads

## Sorting

Use `sort` with comma-separated stable field names.

Ascending:

```text
GET /api/v1/orgs/acme/roles?sort=name
```

Descending:

```text
GET /api/v1/orgs/acme/audit-events?sort=-createdAt
```

Multiple fields:

```text
GET /api/v1/orgs/acme/memberships?sort=status,-createdAt
```

Rules:

- default sort is deterministic
- allow only documented fields
- mutable list defaults should use `-createdAt,-id` or equivalent stable ordering
- if a client sort does not include a unique tie-breaker, the server appends one internally
- expensive sort fields are not exposed
- sorting never changes tenant isolation

## Pagination

Use cursor pagination for mutable or large collections.

Request:

```text
GET /api/v1/orgs/acme/audit-events?limit=50&cursor=opaque-cursor
```

Response:

```json
{
  "data": [],
  "pagination": {
    "limit": 50,
    "nextCursor": "opaque-next-cursor",
    "previousCursor": null,
    "hasMore": true
  }
}
```

Rules:

- default `limit` is `50`
- maximum `limit` is `200` unless endpoint lowers it
- cursors are opaque and client-stored only
- cursors may encode normalized filter, sort, limit, tenant, and last item position
- cursor payloads must be signed or otherwise tamper-resistant if they contain state
- cursor contents are not public contract and may change
- offset paging is allowed only for small stable admin lists when documented
- total counts are omitted by default because they can be expensive

## Expanding

Use `expand` for related data.

Example:

```text
GET /api/v1/orgs/acme/roles?expand=claims
```

Multiple expands:

```text
GET /api/v1/orgs/acme/groups?expand=members,roles
```

Rules:

- expansions are allowlisted per resource
- default expansion is shallow
- do not expand secrets, token plaintext, token hashes, session tokens, ciphertext, or large unbounded child collections
- avoid nested expand chains unless explicitly documented
- expanded collections must be bounded or represented as summaries with links
- expanded data obeys the same authorization and tenant checks as direct endpoints

## Response Metadata

All query responses include `meta.requestId` and `meta.serverTime`. They should include `meta.traceId` when available.

Endpoints may include normalized query echo in `meta.query`.

```json
{
  "meta": {
    "requestId": "req_...",
    "traceId": "...",
    "serverTime": "2026-05-28T12:00:00Z",
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

Rules:

- query echo is normalized, not a raw query string dump
- secrets and bearer values are never echoed
- unknown params rejected by validation do not need a query echo

## Validation Errors

Invalid query requests use the standard error envelope.

```json
{
  "error": {
    "code": "validation_failed",
    "message": "Validation failed.",
    "details": {
      "fields": {
        "sort": ["Unsupported sort field: emailHash."],
        "filter[status]": ["Unsupported value: archived."]
      }
    }
  },
  "meta": {
    "requestId": "req_...",
    "serverTime": "2026-05-28T12:00:00Z"
  }
}
```

## Batch Processing

Use explicit batch endpoints only when needed.

Rules:

- default batch write limit is `25` items
- default batch read limit is `100` items
- bulk operations over `100` items should be async jobs
- batch mutations require permission for every target item
- batch mutations require tenant validation for every referenced id
- batch mutating endpoints should require `Idempotency-Key` when retries are likely
- batch endpoints support dry-run when the operation can make broad changes
- partial success returns per-item results using the batch envelope in `api-contract.md`
- batch-level errors are only for invalid batch requests, not per-item failures

## Rate Limiting

Rate limit dimensions come from `api-contract.md`.

Rules:

- list endpoints with `q`, `expand`, or expensive filters use stricter limits
- mutating endpoints are always rate limited
- batch endpoints have stricter limits than single-resource endpoints
- auth-sensitive endpoints are stricter than read-only endpoints
- high-risk operations such as token creation or secret reveal have endpoint-specific limits

## Payload Budget

- list responses are compact by default
- detail responses stay bounded
- use relationship summaries and links for large related collections
- do not return large nested graphs without explicit expansion
- do not include raw secrets, tokens, hashes, ciphertext, or encrypted blobs
- avoid returning full claim sets in lists unless explicitly expanded and authorized
- prefer async exports for large reports

## Caching

- cache list queries only when filters are stable and invalidation rules are clear
- do not cache highly personalized or highly expanded payloads for long
- cache keys include normalized filter, search, sort, limit, cursor, expand, tenant, actor, and authorization version
- invalidate relevant query caches on role, group, membership, session, API key, service account, and permission changes

## Endpoint Contract Checklist

Every new list endpoint documents:

- default sort
- supported filters and operators
- supported sort fields
- supported expansions
- default and maximum `limit`
- whether `q` is supported
- rate limit category
- response resource type and compact/detail fields

Every new batch endpoint documents:

- max item count
- dry-run support
- partial success or all-or-nothing behavior
- idempotency behavior
- async threshold
