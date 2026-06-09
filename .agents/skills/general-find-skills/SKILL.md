---
name: general-find-skills
description: Discover external skills from skills.sh, compare install counts and source quality, and decide whether to add local adapted skills; use when looking for reusable agent skills.
---

# General Find Skills

Adapted from `vercel-labs/skills@find-skills` on skills.sh.

Use this when deciding whether this repo needs another skill.

## Search Process

- Check skills.sh for the domain.
- Prefer official or reputable sources.
- Prefer higher install counts when quality looks comparable.
- Check whether the skill is framework-specific or conflicts with this repo.
- Do not add a skill just because it exists.

## Add Strategy

- Use external skills for generic guidance.
- Add local adapted skills for project-specific behavior, commands, contracts, or constraints.
- Keep skill bodies short and actionable.
- Include source links or names for review.
- Make repo-specific skills win on conflicts.

## This Repo

- Add local skills under `.agents/skills/<name>/SKILL.md`.
- Keep names lowercase and hyphenated.
- Include `name` and `description` frontmatter.
