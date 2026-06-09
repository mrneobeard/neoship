# Identity Query Contract

## Purpose

Define one horizontal rule set for list/detail endpoints across identity APIs.

Goal: keep responses small by default, support useful querying, and avoid expensive or leaky payloads.

## Default Shape

- list endpoints return compact summary objects by default
- detail endpoints return full object data, but still omit secrets unless explicitly requested and safe
- related data is opt-in via `expand`
- server-side filtering, sorting, and paging are required for any collection that can grow beyond a handful of rows

## Filtering

- use `filter[...]` for query filters
- only expose filters that are indexed or cheap to evaluate
- support equality first, then prefix/contains only when justified
- do not expose ad hoc arbitrary query expressions
- filter names should match stable domain names, not database column names

Examples:

- `filter[name]=admin`
- `filter[status]=active`
- `filter[orgId]=...`

## Sorting

- use `sort` with stable field names
- default sort should be deterministic
- allow only allowlisted fields
- sort should be backed by an index when practical
- if a field is expensive to sort on, do not expose it

Examples:

- `sort=name`
- `sort=-createdAt`

## Paging

- all list endpoints must support paging once the collection can grow
- default page size: 50
- maximum page size: 100 unless a resource explicitly lowers it
- prefer cursor paging for mutable or large collections
- use offset paging only when the collection is small and stable
- return enough paging metadata for the client to continue

## Expanding

- use `expand` for related objects
- expansion must be allowlisted per resource
- default to shallow expansion only
- do not expand secrets or large child collections by default
- avoid nested expand chains unless a resource explicitly permits them

Examples:

- `expand=claims`
- `expand=members`
- `expand=roles,claims`

## Batch Processing

- use explicit batch endpoints only when needed
- batch requests must have strict item limits
- default batch size limit: 25 for writes, 100 for reads
- batch mutations must be partial-failure aware or explicitly all-or-nothing
- batch mutating endpoints should require `Idempotency-Key` when retries are likely
- return per-item results for partial success

## Rate Limiting

- list endpoints with `expand` or expensive filters should be rate limited more aggressively
- mutating endpoints should always be rate limited
- batch endpoints should have their own stricter limits
- auth-sensitive endpoints should be protected more aggressively than read-only endpoints

## Payload Budget

- do not return large nested graphs without explicit expand
- do not include raw secrets, tokens, or hashes in list responses
- avoid returning entire claim sets unless explicitly requested
- prefer counts, summaries, and links for large collections

## Caching

- cache list queries only when they have stable filters and clear invalidation rules
- do not cache highly personalized or highly expanded payloads for long
- cache keys must include filter, sort, paging, and expand parameters when used

## Implementation Rules

- every new list endpoint should define its paging and sort defaults in the endpoint contract
- every new expand target must be documented and allowlisted
- every new batch endpoint must document max item count and retry semantics
- if a query can become expensive, add a rate limit and consider an alternate subresource endpoint
