---
name: general-verification
description: Evidence-before-claims, completion checks, build/test verification, and honest status reporting; use before saying work is done or stable.
---

# General Verification

Adapted from `obra/superpowers@verification-before-completion` on skills.sh.

Use this before claiming work is complete.

## Rule

- Do not claim a build, test, fix, or behavior passes without fresh evidence.

## Gate

- Identify the proof command or manual check.
- Run it if feasible.
- Read the output and exit code.
- Make the claim match the evidence.
- If verification was skipped, say why.

## Practical Scope

- Use the smallest verification that proves the change.
- Use broader verification for shared code, auth, DB, migrations, and low-level libraries.
- Do not run expensive full suites when a narrow check gives enough confidence, unless the change risk requires it.

## Project Fit

- For config/docs-only changes, file verification may be enough.
- For code changes, run relevant tests per `neo-testing` and `pragmatic-regression-testing`.
