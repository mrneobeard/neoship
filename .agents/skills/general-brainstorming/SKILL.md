---
name: general-brainstorming
description: Collaborative idea shaping, requirements clarification, design tradeoffs, and short specs; use when the user wants to think through a feature before implementation.
---

# General Brainstorming

Adapted from `obra/superpowers@brainstorming` on skills.sh.

Use this only when the user is exploring, comparing options, or explicitly asking for design thinking. Do not use it to delay obvious implementation work.

## Process

- Understand the current project context first.
- Ask one focused question at a time only when needed.
- Separate known facts from assumptions.
- Prefer concrete tradeoffs over abstract architecture talk.
- End with a short decision or spec the user can approve.

## Output

- Keep designs short unless the user asks for depth.
- Call out the smallest shippable version.
- Identify risky unknowns.
- Do not convert brainstorming into heavy planning unless requested.

## Project Fit

- For identity/API ideas, ground the discussion in `.agents/context/contracts/*`.
- For multi-db/runtime ideas, ground it in `.agents/context/design/multi-db-apphost-plan.md`.
- For UI ideas, pair with `general-frontend-design` and `neo-ui-product-design`.
