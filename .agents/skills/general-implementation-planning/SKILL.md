---
name: general-implementation-planning
description: Practical implementation planning, task slicing, sequencing, risk control, and shipping-focused execution plans; use for multi-step work that needs a plan.
---

# General Implementation Planning

Adapted from `obra/superpowers@writing-plans` on skills.sh, but simplified for this repo's shipping bias.

Use this when work is complex enough that sequencing matters. Do not create giant plans for simple edits.

## Plan Shape

- State the outcome.
- List the smallest useful phases.
- Identify files or areas likely to change.
- Put build blockers before feature layering.
- Put stateful/data mutations before UI polish.
- Include verification only where it gives signal.

## Task Slicing

- Prefer vertical slices that produce working behavior.
- Keep each slice reviewable.
- Avoid horizontal churn across many files before anything works.
- Avoid speculative abstractions.

## Testing Bias

- Do not require TDD.
- Plan high-signal regression, integration, state mutation, and low-level tests.
- Skip low-value tests that only exercise implementation details.

## Project Fit

- Identity work starts from model/build health and contracts.
- EF work keeps provider-specific differences isolated.
- UI work starts with user flow and responsive layout.
