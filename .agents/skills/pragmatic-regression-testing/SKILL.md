---
name: pragmatic-regression-testing
description: Pragmatic testing for shipping, regressions, stability, integration flows, mutating state, and low-level components; use instead of TDD guidance in this repo.
---

# Pragmatic Regression Testing

This repo does not want TDD as a default. Use tests for signal, stability, and shipping confidence.

## Priority Order

- Integration tests for real product behavior.
- Regression tests for bugs that already happened or are likely to happen.
- Stateful mutation tests where object or database state can drift.
- Low-level unit tests for crypto, parsing, protocol, secrets, and allocation-sensitive code.
- UI flow tests for critical user experience.
- Smoke tests for build/startup/migrations.

## High-Value Test Targets

- Auth flows: signup, login, logout, session validation, revocation.
- Token and secret handling: hashing, verification, reset/verification tokens.
- EF mutations: create/update/revoke/disable flows and migration application.
- State transitions: statuses, lockouts, expiration, revocation, retries.
- Low-level libraries: crypto vectors, malformed inputs, boundary conditions, memory clearing.
- UI: forms, navigation, loading/error states, mobile layout, critical accessibility.

## What To Avoid

- Tests that only prove mocks were called.
- Brittle implementation-detail tests.
- Huge test suites that slow shipping without catching regressions.
- Coverage for coverage's sake.
- TDD ceremony when it adds no value.

## Coverage Goal

- Decent coverage is good when it maps to user confidence.
- Prefer fewer high-signal tests over many low-signal tests.
- If a test would not catch a real regression, question it.

## Verification

- Run the smallest relevant test set first.
- Run broader tests when touching shared state, auth, DB, or low-level libraries.
- Report what was run and what confidence it gives.
