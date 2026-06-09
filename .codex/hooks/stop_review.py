#!/usr/bin/env python3
import hashlib
import json
import os
import sys
from pathlib import Path


def main() -> int:
    payload = json.load(sys.stdin)
    state = read_state(payload)

    if state.get("code_edit_seen") and not state.get("verification_seen"):
        print(
            json.dumps(
                {
                    "continue": True,
                    "systemMessage": (
                        "Repository policy: code/config edits were detected in this Codex session, "
                        "but no build/test/format/check command was recorded. Run relevant verification "
                        "or explicitly report why it was skipped."
                    ),
                }
            )
        )

    return 0


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


if __name__ == "__main__":
    raise SystemExit(main())
