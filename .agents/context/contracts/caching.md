# Identity Caching Contract

## Purpose

Use cache for speed, not as the source of truth.

Canonical truth remains the database model in `apps/api/Data.Model`.

## Cacheable Reads

### Session Lookup

- cache by session token digest
- TTL: 1-5 minutes
- invalidate on revoke, expiry, password reset-all, or user suspension

### API Key Lookup

- cache by key digest
- TTL: 1-5 minutes
- invalidate on revoke, expiry, claim change, role/group change if resolved permissions depend on them

### Permission Resolution

- cache resolved permission sets per actor/org/scope
- TTL: max 60 seconds
- invalidate on:
  - role change
  - group membership change
  - claim change
  - service account change
  - user suspension

### Identity Provider Metadata

- cache OIDC discovery and JWKS
- TTL: provider-driven if possible, else 15 minutes
- force refresh on provider update

### Known Network Summary

- cache recent known-network state for risk scoring
- TTL: 5-15 minutes
- invalidate on successful login that updates `UserKnownNetwork`

## Things Not To Cache Long-Term

- password reset tokens
- email verification tokens
- MFA enrollment state beyond short challenge windows
- full role/group lists in client tokens

## Challenge Storage

Short-lived auth challenges may use cache if durability is not required:

- passkey registration challenge
- passkey login challenge
- TOTP setup challenge

TTL: 5 minutes or less.

## Security Rules

- cache stores digests and ids, not plaintext secrets
- if distributed cache is used, treat it as sensitive infrastructure
- revocation paths must invalidate cache before returning success where practical
