---
name: general-shadcn-svelte
description: shadcn-svelte style components, Svelte 5 composition, Tailwind variants, component source ownership, and UI primitive usage; use when adding or editing shadcn-style components.
---

# General Shadcn Svelte

Adapted from skills found on skills.sh:

- `shadcn/ui@shadcn`
- `vercel-labs/json-render@shadcn-svelte`

Use this for shadcn-style component work. The official shadcn skill is React-focused, so apply only the source-owned component principles to Svelte.

## Principles

- Components are source code owned by this app, not opaque library calls.
- Preserve local modifications when updating or adding components.
- Use project aliases and existing component paths.
- Prefer composition over prop explosion.
- Keep primitives accessible by default.

## Svelte Fit

- Use Svelte 5 runes and snippets.
- Type component props.
- Use `bind:this` and `ref` patterns only where needed.
- Use `tailwind-variants`, `clsx`, and `tailwind-merge` consistently with existing components.

## Styling

- Use semantic tokens and variants.
- Include focus, disabled, invalid, hover, and active states.
- Avoid one-off styles when a variant belongs in the component definition.
- Keep icon sizing predictable.

## Project Fit

- Existing components live under `apps/ui/src/lib/components/ui`.
- Existing button code is a good local pattern to follow.
