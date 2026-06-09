---
name: general-playwright
description: Playwright E2E and browser testing, selectors, fixtures, flaky test prevention, UI flows, and regression coverage; use when adding or debugging Playwright tests.
---

# General Playwright

Adapted from skills found on skills.sh:

- `currents-dev/playwright-best-practices-skill@playwright-best-practices`
- `anthropics/skills@webapp-testing`

Use this for browser and E2E tests. Pair with `pragmatic-regression-testing` and `neo-testing`.

## What To Test

- Critical user journeys.
- Auth/session flows.
- Stateful UI flows where regression risk is high.
- Forms with validation, async save, optimistic state, or error recovery.
- Mobile and desktop layout for important screens.

## Test Style

- Prefer user-visible selectors and accessible roles.
- Avoid brittle selectors tied to incidental DOM structure.
- Wait for user-observable states, not arbitrary timeouts.
- Keep tests independent and deterministic.
- Capture console errors when debugging UI failures.

## Flake Control

- Do not depend on test order.
- Isolate state per test where practical.
- Mock third-party services when they are outside the behavior under test.
- Prefer real app behavior for first-party flows.

## Project Fit

- UI E2E tests live in `apps/ui` and use Playwright.
- Run the relevant UI check or test script before claiming the flow is stable.
