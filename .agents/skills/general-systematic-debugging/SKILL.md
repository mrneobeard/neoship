---
name: general-systematic-debugging
description: Root-cause debugging, failure reproduction, hypothesis testing, diagnostics, and avoiding random fixes; use when diagnosing bugs or test failures.
---

# General Systematic Debugging

Adapted from `obra/superpowers@systematic-debugging` on skills.sh.

Use this when something fails.

## Rule

- Do not patch symptoms before identifying a likely root cause.

## Loop

- Reproduce the failure or find the closest available evidence.
- Read the error and stack trace carefully.
- Trace data flow backward from the observed bad state.
- Form one hypothesis at a time.
- Test the hypothesis with the smallest useful command or code inspection.
- Implement the smallest fix that addresses the root cause.
- Add or update a regression test when the bug is important enough.

## Stop Conditions

- After repeated failed fixes, stop and reassess the model.
- If the failure crosses API, database, UI, or external service boundaries, map the boundary before editing.
- If the data is sensitive, avoid logging secrets while debugging.

## Project Fit

- For auth and identity failures, inspect audit, request context, token digest, session state, and EF state.
- For UI failures, inspect Svelte runtime behavior, browser console, and Playwright traces when available.
