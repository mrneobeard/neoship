---
name: general-svelte5
description: General Svelte 5, runes, SvelteKit, snippets, events, TypeScript props, and Svelte MCP docs; use for Svelte work before applying project UI rules.
---

# General Svelte5

Adapted from skills found on skills.sh:

- `sveltejs/ai-tools@svelte-code-writer`
- `sveltejs/ai-tools@svelte-core-bestpractices`
- `ejirocodes/agent-skills@svelte5-best-practices`

Use this for generic Svelte 5 judgment. If it conflicts with `neo-sveltekit-svelte5`, that skill wins.

## Svelte MCP

- For Svelte or SvelteKit questions, use Svelte docs first when tools are available.
- Fetch relevant documentation sections before relying on memory.
- Use the Svelte autofixer after writing Svelte code and repeat until clean.

## Runes

- Use `$state` only for reactive values that update markup, `$derived`, or `$effect`.
- Use normal variables for non-reactive local values.
- Prefer `$derived` for computed values.
- Use `$effect` for side effects, not derived state.
- Use `$state.raw` for large objects or arrays that are reassigned rather than deeply mutated.

## Components

- Type props explicitly with TypeScript.
- Use snippets and `{@render ...}` for Svelte 5 composition.
- Prefer callback props and normal DOM events over legacy event patterns.
- Keep components small enough to understand without creating excessive abstraction.

## SvelteKit

- Keep server-only code in server modules.
- Use load functions and form actions where they improve UX and server correctness.
- Respect SSR boundaries and avoid browser-only APIs during SSR.
