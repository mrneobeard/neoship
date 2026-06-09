---
name: neo-js-ts-tooling
description: JavaScript, TypeScript, package scripts, pnpm, npm, Vite+, oxformat, and Node tooling; use when editing TS/JS config, package.json, or frontend tooling.
---

# Neo JS TS Tooling

Use this skill for TypeScript, JavaScript, package scripts, and toolchain work.

## Package Manager Rules

- Check the nearest `package.json` and `AGENTS.md`; do not assume one package manager globally.
- Root has `pnpm-workspace.yaml` and uses pnpm workspace commands.
- `apps/ui/AGENTS.md` says UI package manager is npm, but root tasks use pnpm from the repo root.
- `apps/apphost` has its own `package.json` and `package-lock.json`.

## Tooling Facts

- Root `package.json` is ESM and private.
- UI uses Vite+, Vite, Vitest, Playwright, SvelteKit, Tailwind.
- AppHost uses TypeScript NodeNext and Aspire TypeScript SDK.
- Formatting config: `oxfmt.config.ts` with `printWidth: 120`.

## Editing Rules

- Keep scripts and dependency changes scoped to the owning package.
- Prefer catalog versions in `pnpm-workspace.yaml` where existing dependencies use `catalog:`.
- Do not mix lockfile/package-manager changes unless the task requires it.
- Preserve ESM imports and NodeNext module behavior.

## Validation

- UI: run the relevant `apps/ui` script or Vite+ command.
- AppHost: run `pnpm --dir apps/apphost run build` or the package's own build command.
- Formatting: use oxfmt where relevant.
