---
name: general-tailwind-v4
description: Tailwind CSS v4, design tokens, variants, responsive utilities, component styling, and accessible utility patterns; use when editing Tailwind-heavy UI.
---

# General Tailwind V4

Adapted from `wshobson/agents@tailwind-design-system` on skills.sh.

Use this for Tailwind design-system judgment. Pair with `neo-sveltekit-svelte5` in this repo.

## Tailwind V4 Defaults

- Prefer CSS-first tokens and native CSS variables where the project uses them.
- Use semantic tokens before raw one-off colors.
- Keep component variants explicit and named.
- Use responsive utilities intentionally, not as patchwork.
- Use `size-*` shorthand where it improves clarity.

## Component Styling

- Keep base, variant, and size concerns separate.
- Use variant helpers when existing components use them.
- Keep focus-visible and disabled states in the base style.
- Prefer semantic state selectors such as `aria-*` and `data-*` where useful.

## Accessibility

- Preserve visible focus rings.
- Keep target sizes usable.
- Ensure disabled and error states are perceivable.
- Do not remove native semantics just to style an element.

## Project Fit

- This repo uses Tailwind 4 with `@tailwindcss/vite`.
- Existing UI components use `tailwind-variants`, `clsx`, and `tailwind-merge`.
