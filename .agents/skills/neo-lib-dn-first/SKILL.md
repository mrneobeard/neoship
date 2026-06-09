---
name: neo-lib-dn-first
description: Neo base class library reuse, lib/dn Core, Crypto, SSH, Age, Exec, Secrets, and avoiding duplicate utility code; use before adding common helpers or low-level behavior.
---

# Neo Lib Dn First

Use this skill before adding helper code, low-level code, crypto, shell/process helpers, options/results, or secret handling.

## Library Map

- Core primitives: `lib/dn/Core/src`.
- Result and option flow: `lib/dn/Core/src/Results`, `lib/dn/Core/src/Options`.
- Cryptography: `lib/dn/Crypto/src`.
- Public-key file encryption: `lib/dn/Age/src`.
- SSH protocol and keys: `lib/dn/Ssh/src`.
- Secret generation/masking/storage abstractions: `lib/dn/Secrets/src` and `lib/dn/Secrets.Keyring/src`.
- Process execution: `lib/dn/Exec/src`.

## Rules

- Search `lib/dn` before adding a new general helper.
- Prefer existing Core `Result`, `ValueResult`, `Option`, and `ValueOption` over ad hoc null/error patterns.
- Prefer existing Crypto/Age/SSH code for cryptographic work.
- Prefer existing Secrets code for masking, generation, and secret handling.
- Do not duplicate protocol parsing, command execution, or cryptographic primitives unless the existing library is insufficient.

## When Existing Code Is Not Enough

- Extend the smallest relevant library.
- Add high-signal low-level tests.
- Keep public APIs documented per repo XML doc rules.
- Avoid dependencies that duplicate existing lib/dn functionality.
