---
name: neo-sveltekit-svelte5
description: SvelteKit, Svelte 5 runes, Svelte MCP docs/autofixer, Tailwind, shadcn-svelte, and UI app work; use when editing apps/ui Svelte or frontend code.
---

# Neo SvelteKit Svelte5

Use this skill for `apps/ui`.

## Required Context

- Read `apps/ui/AGENTS.md`.
- Use the Svelte MCP docs flow for Svelte or SvelteKit questions.
- Use the Svelte autofixer whenever writing Svelte code, and repeat until clean.

## Project Facts

- SvelteKit app lives in `apps/ui`.
- Svelte 5 runes mode is forced for project code in `svelte.config.js`.
- Tailwind CSS 4 is used through `@tailwindcss/vite`.
- shadcn-svelte style components exist under `src/lib/components/ui`.
- Example button component uses `tailwind-variants`, `clsx`, and `tailwind-merge`.

## Coding Rules

- Use runes such as `$props()` and `$bindable()` for component state/props.
- Preserve existing shadcn/Tailwind component patterns.
- Prefer simple components with accessible markup.
- Keep server-only code out of client bundles.
- Do not add broad state-management libraries unless requested.

## Validation

- In `apps/ui`, use Vite+ commands from `apps/ui/AGENTS.md` when available: `vp check` and `vp test`.
- From repo root, package scripts are available through `pnpm --dir apps/ui run check`, `pnpm --dir apps/ui run test:unit -- --run`, and `pnpm --dir apps/ui run test:e2e`.
- For E2E changes, run Playwright tests or explain why they were not run.
