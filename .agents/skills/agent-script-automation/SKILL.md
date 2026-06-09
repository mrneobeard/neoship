---
name: agent-script-automation
description: Repeated tool-call automation with Deno or uv Python scripts in .agents/scripts, smart defaults, user review before commit, and Cast task promotion when useful to developers.
---

# Agent Script Automation

Use this skill when repeated searches, reads, checks, or transformations are becoming noisy.

## Rule

- If the same tool-call sequence is used more than three times, create a script and use it.
- Store scripts in `.agents/scripts`.
- Scripts must be reviewed by the user before committing.
- Prefer Deno for TypeScript scripts.
- Prefer uv for Python scripts.
- If the workflow is useful to a developer, add or propose a Cast task instead of keeping it agent-only.

## When To Script

- Repeated repo scans with the same filters.
- Repeated validation or report generation.
- Repeated markdown/frontmatter checks.
- Repeated migration, package, or project inventory tasks.
- Repeated codegen-safe transformations.
- Security scans of skills, configs, or generated files.

## When Not To Script

- One-off inspection.
- A small direct tool call is clearer.
- The script would hide risky behavior.
- The script needs secrets or privileged access.
- The script would duplicate an existing Cast task.

## Script Requirements

- Use smart defaults that work from repo root.
- Accept explicit path arguments to narrow scope.
- Print concise, useful output.
- Exit nonzero for real failures.
- Avoid destructive writes by default.
- Provide a dry-run mode when the script can write.
- Keep permissions narrow for Deno.
- Keep Python dependencies explicit and minimal for uv.
- Include a short header comment explaining purpose and safe usage.

## Promotion To Cast

- If a script helps normal developer workflow, add a task to `castfile`.
- Cast task names should fit existing namespaces: `build`, `test`, `lint`, `audit`, `ui`, `dotnet`, `aspire`.
- Keep Cast tasks discoverable with `desc`.
- Prefer Cast for stable tasks and `.agents/scripts` for agent-only helpers.

## Review Gate

- Before committing a new script, summarize what it reads, writes, and executes.
- Ask the user to review the script.
- Do not commit unreviewed scripts.
