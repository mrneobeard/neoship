---
name: small-plan-small-change
description: Planning, implementation order, minimal edits, ticket breakdown, and avoiding over-engineering; use when asked for a plan or when a task has several possible approaches.
---

# Small Plan Small Change

Use this skill to keep work practical.

## Planning Rules

- Read existing context before designing.
- Prefer the smallest correct change.
- Keep things in one function unless reuse is real.
- Do not add compatibility paths unless persisted data, shipped behavior, or external consumers require them.
- If unclear, ask one short question.

## Repo Planning Sources

- Design docs: `.agents/context/design/*`.
- Work plans and tickets: `.agents/context/work/*`.
- Contracts: `.agents/context/contracts/*`.
- Current code is the final source of truth when docs drift.

## Implementation Order

- Fix build blockers before feature layering.
- For identity work, make `Data.Model` healthy before expanding API flows.
- For multi-db work, keep provider overrides small before adding migrations.
- For UI work, establish layout/components before complex state.

## Output

- Use short numbered steps for plans.
- Call out blockers and decision points.
- Avoid long risk lists unless requested.
