---
name: neo-ui-product-design
description: UI design, product screens, responsive layout, accessibility, Tailwind visual design, and avoiding generic AI-looking pages; use when creating or changing user-facing UI.
---

# Neo UI Product Design

Use this skill for UI design work in `apps/ui`.

## Design Direction

- Preserve existing component patterns when present.
- The current app is starter-level, so future identity/product UI can establish a stronger visual system.
- Avoid generic dashboard cards and interchangeable SaaS layouts.
- Design for desktop and mobile from the start.
- Prefer clear hierarchy, strong typography, intentional spacing, and useful empty/error states.

## Accessibility

- Use semantic HTML first.
- Ensure keyboard-visible focus states.
- Use labels for form controls.
- Preserve color contrast.
- Avoid hiding critical state only in color.

## Frontend Stack

- Pair with `neo-sveltekit-svelte5` for implementation.
- Use Tailwind CSS 4 utilities and existing shadcn-svelte components.
- Reuse `cn` and variant helpers where appropriate.

## Validation

- Check desktop and mobile layout.
- Run Svelte checks/tests relevant to the changed UI.
- For interactive flows, add tests or manual verification notes.
