#!/usr/bin/env python3
import hashlib
import json
import os
import re
import subprocess
import sys
from pathlib import Path

DANGEROUS_COMMAND_PATTERNS = [
    re.compile(r"\brm\s+[^\n;]*-rf\s+(?:/|~|\$HOME|\.|\.\.|\*)(?:\s|$)"),
    re.compile(r"\bgit\s+reset\s+--hard\b"),
    re.compile(r"\bgit\s+clean\s+-[^\n;]*[xfd][^\n;]*"),
    re.compile(r"\bgit\s+checkout\s+--\s+"),
    re.compile(r"\bchmod\s+(?:-R\s+)?777\b"),
    re.compile(r"\bmkfs(?:\.|\s)"),
    re.compile(r"\bdd\s+if="),
    re.compile(r"\b(?:shred|wipefs)\b"),
]


def main() -> int:
    payload = json.load(sys.stdin)
    tool_name = str(payload.get("tool_name") or "")
    tool_input = payload.get("tool_input") or {}
    command = str(tool_input.get("command") or "") if isinstance(tool_input, dict) else ""

    if tool_name == "Bash":
        for pattern in DANGEROUS_COMMAND_PATTERNS:
            if pattern.search(command):
                return deny(f"Repository policy blocks dangerous shell command pattern: {pattern.pattern}")

        if touches_secret_env(command):
            return deny("Repository policy blocks shell access to secret .env files. Use .env.example or ask the user.")

        if re.search(r"\bgit\s+commit\b", command):
            if staged_whitespace_errors(payload.get("cwd")):
                return deny("Repository policy blocks git commit while staged whitespace errors exist. Run git diff --cached --check.")

            state = read_state(payload)
            if state.get("code_edit_seen") and not state.get("verification_seen"):
                return deny(
                    "Repository policy blocks agent git commit after code/config edits without verification. "
                    "Run relevant build, test, format, or check commands first."
                )

    if tool_name in {"apply_patch", "Edit", "Write"} and touches_secret_env(command):
        return deny("Repository policy blocks edits to secret .env files. Use .env.example or ask the user.")

    return 0


def deny(reason: str) -> int:
    print(
        json.dumps(
            {
                "hookSpecificOutput": {
                    "hookEventName": "PreToolUse",
                    "permissionDecision": "deny",
                    "permissionDecisionReason": reason,
                }
            }
        )
    )
    return 0


def touches_secret_env(text: str) -> bool:
    matches = re.findall(r"(?:^|[\s'\"])(?:\.?\.?[/\\])?(?:[\w.-]+[/\\])*\.env(?:\.[\w-]+)?(?=$|[\s'\"])", text)
    return any(".env.example" not in match for match in matches)


def state_file(payload: dict) -> Path:
    raw_key = f"{payload.get('session_id', '')}:{payload.get('cwd', '')}"
    key = hashlib.sha256(raw_key.encode("utf-8")).hexdigest()
    root = Path(os.environ.get("TMPDIR", "/tmp")) / "neoship-codex-hooks"
    return root / f"{key}.json"


def read_state(payload: dict) -> dict:
    path = state_file(payload)
    if not path.exists():
        return {}

    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError:
        return {}


def staged_whitespace_errors(cwd: object) -> bool:
    run_cwd = cwd if isinstance(cwd, str) and cwd else None
    result = subprocess.run(["git", "diff", "--cached", "--check"], cwd=run_cwd, capture_output=True, text=True, check=False)
    return result.returncode != 0


if __name__ == "__main__":
    raise SystemExit(main())
