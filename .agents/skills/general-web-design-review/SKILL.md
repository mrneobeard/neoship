---
name: general-web-design-review
description: General web UI review, accessibility, interaction quality, visual consistency, and interface guideline checks; use when reviewing or polishing frontend files.
---

# General Web Design Review

Adapted from `vercel-labs/agent-skills@web-design-guidelines` on skills.sh.

Use this to audit UI work after implementation.

## Review Order

- Check task fit: does the UI solve the user problem?
- Check hierarchy: can users see primary, secondary, and destructive actions?
- Check responsiveness: desktop and mobile both work.
- Check accessibility: labels, focus, semantics, contrast, keyboard flow.
- Check consistency: spacing, type scale, colors, radius, icons, and component variants.
- Check interaction states: hover, focus, active, loading, disabled, error, empty.

## Findings Style

- Prefer concrete findings with file and line when reviewing code.
- Explain the user impact.
- Recommend the smallest useful fix.
- Do not nitpick subjective taste unless it hurts clarity, consistency, or UX.

## Project Fit

- For `apps/ui`, use SvelteKit, Tailwind 4, and existing shadcn-svelte patterns.
- For new product screens, avoid generic AI layouts and use `neo-ui-product-design`.
