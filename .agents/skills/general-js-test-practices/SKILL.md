---
name: general-js-test-practices
description: JavaScript and TypeScript test structure, async tests, mocks, fixtures, and high-signal frontend test design; use when writing Vitest or JS/TS tests.
---

# General JS Test Practices

Adapted from `github/awesome-copilot@javascript-typescript-jest` on skills.sh, translated for this repo's Vitest/Playwright stack.

Use this for JS/TS tests. Pair with `pragmatic-regression-testing`.

## Test Structure

- Use descriptive test names that state behavior.
- Keep tests close to the code or existing test pattern.
- Organize related behavior with `describe` blocks when it improves readability.
- Prefer one clear behavior assertion group over many unrelated assertions.

## Async Tests

- Use `async` and `await` directly.
- Await user-visible state changes.
- Avoid arbitrary sleeps.
- Use realistic fixtures for stateful behavior.

## Mocking

- Mock external APIs, time, random values, and slow services when needed.
- Do not mock internal implementation just to assert calls.
- Prefer real first-party code paths for regression coverage.

## Project Fit

- This repo uses Vitest, browser Playwright provider, and Playwright E2E.
- Jest-specific APIs are not the default here.
