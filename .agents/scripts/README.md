# Agent Scripts

Agent helper scripts live here.

## Rules

- Create a script when the same tool-call sequence is used more than three times.
- Use Deno for TypeScript scripts.
- Use uv for Python scripts.
- Scripts need smart defaults that work from the repo root.
- Scripts that write files need dry-run behavior by default or a clear write flag.
- Scripts must be reviewed by the user before committing.
- If a script is useful to developers, add or propose a Cast task in `castfile`.

## Safety

- Avoid secrets.
- Avoid privileged operations.
- Avoid destructive writes.
- Keep dependency use minimal and explicit.
- Print what the script will read and write when it performs mutations.
