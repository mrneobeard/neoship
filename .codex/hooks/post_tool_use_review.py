#!/usr/bin/env python3
import hashlib
import json
import os
import re
import sys
from pathlib import Path

CODE_PATH_PATTERN = re.compile(r"\.(?:cs|csproj|props|targets|slnx|ts|tsx|mts|js|mjs|svelte|css|json|jsonc|toml|ya?ml)$", re.IGNORECASE)
VERIFY_COMMAND_PATTERN = re.compile(
    r"\b(?:dotnet\s+(?:test|build|format)|pnpm\b[^\n;]*(?:\srun\s+)?(?:test|build|check|lint|format|audit)|"
    r"npm\s+run\s+(?:test|build|check|lint)|vp\s+(?:test|check|build)|playwright\s+test|oxfmt\b|"
    r"git\s+diff\s+--cached\s+--check)\b"
)


def main() -> int:
    payload = json.load(sys.stdin)
    state = read_state(payload)
    tool_name = str(payload.get("tool_name") or "")
    tool_input = payload.get("tool_input") or {}

    if isinstance(tool_input, dict):
        command = str(tool_input.get("command") or "")
        if tool_name == "Bash" and VERIFY_COMMAND_PATTERN.search(command):
            state["verification_seen"] = True
            state["last_verification_command"] = command

        if tool_name in {"apply_patch", "Edit", "Write"} and looks_like_code_edit(tool_input, command):
            state["code_edit_seen"] = True

    write_state(payload, state)
    return 0


def looks_like_code_edit(tool_input: dict, command: str) -> bool:
    for key in ("filePath", "path"):
        value = tool_input.get(key)
        if isinstance(value, str) and CODE_PATH_PATTERN.search(value):
            return True

    for line in command.splitlines():
        match = re.match(r"^(?:(?:\+\+\+|---) [ab]/(.+)|\*\*\* (?:Add|Update|Delete) File: (.+))$", line)
        file_path = (match.group(1) or match.group(2)) if match else ""
        if CODE_PATH_PATTERN.search(file_path):
            return True

    return False


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


def write_state(payload: dict, state: dict) -> None:
    path = state_file(payload)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(state, sort_keys=True), encoding="utf-8")


if __name__ == "__main__":
    raise SystemExit(main())
