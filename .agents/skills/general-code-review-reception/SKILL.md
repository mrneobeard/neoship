---
name: general-code-review-reception
description: Handling code review feedback, validating reviewer claims, prioritizing fixes, and pushing back with technical evidence; use when applying review comments.
---

# General Code Review Reception

Adapted from `obra/superpowers@receiving-code-review` on skills.sh.

Use this when the user gives review feedback or asks to address review comments.

## Rules

- Verify feedback against the actual code before changing it.
- Ask for clarification if the requested change is ambiguous.
- Fix blocking correctness/security issues first.
- Push back when feedback conflicts with repo rules, breaks behavior, or adds needless complexity.
- Test each meaningful fix when practical.

## Response Pattern

- State which comments are valid and why.
- State which comments need clarification or are risky.
- Apply changes in priority order.
- Report what changed and what was verified.

## Project Fit

- Use repo-specific skills before generic preferences.
- Never revert unrelated user changes while handling review feedback.
