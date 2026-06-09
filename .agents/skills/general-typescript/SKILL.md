---
name: general-typescript
description: General TypeScript, advanced types, generics, discriminated unions, utility types, API shapes, and type-safe configuration; use when editing TypeScript or JS tooling.
---

# General TypeScript

Adapted from skills found on skills.sh:

- `wshobson/agents@typescript-advanced-types`
- `github/awesome-copilot@javascript-typescript-jest`

Use this for TypeScript design. If it conflicts with `neo-js-ts-tooling` or `neo-sveltekit-svelte5`, the Neo skill wins.

## Type Design

- Model domain states with discriminated unions when branches are real.
- Use generics for reusable APIs, not for cleverness.
- Use mapped, conditional, and template literal types only when they reduce runtime bugs or repetition.
- Prefer explicit public types for exported APIs.
- Keep inferred types for local obvious values.

## Safety

- Avoid `any`; use `unknown` and narrow when needed.
- Prefer parsing/validation at boundaries over trusting external data.
- Keep config strongly typed.
- Use exhaustive checks for state machines and protocol-like values.

## Tests

- This repo uses Vitest and Playwright, not Jest, for UI.
- Prefer behavior and integration tests over mocks-heavy unit tests.
- Mock external services only when using the real service is slow, flaky, costly, or outside scope.
