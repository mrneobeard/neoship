---
name: general-web-performance
description: Web performance, Core Web Vitals, loading speed, runtime efficiency, resource budgets, image/font/script optimization; use when frontend performance matters.
---

# General Web Performance

Adapted from `addyosmani/web-quality-skills@performance` on skills.sh.

Use this for frontend performance review and optimization.

## Priorities

- Optimize measured bottlenecks first.
- Protect Core Web Vitals: LCP, CLS, INP.
- Keep JavaScript and CSS payloads small.
- Avoid layout thrashing and unnecessary reactive work.
- Prefer progressive loading and useful skeleton/empty states.

## Loading

- Prioritize critical content and fonts.
- Use responsive images and modern formats when assets are introduced.
- Defer noncritical scripts.
- Avoid heavy dependencies for small UI needs.

## Runtime

- Avoid unnecessary deep reactivity for large Svelte state.
- Debounce expensive input-driven work.
- Use virtualization for large lists when needed.
- Avoid repeated DOM measurement and mutation loops.

## Project Fit

- This repo uses SvelteKit and Tailwind.
- Pair with `general-svelte5` for runes and reactivity performance.
