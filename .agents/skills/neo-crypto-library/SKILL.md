---
name: neo-crypto-library
description: NeoBeard Crypto, password hashing, symmetric encryption, memory protection, constant-time comparisons, secret buffers, and using lib/dn cryptography instead of ad hoc crypto.
---

# Neo Crypto Library

Use this skill for cryptography and secret handling.

## Prefer Existing Libraries

- Crypto primitives: `lib/dn/Crypto/src`.
- Secret helpers: `lib/dn/Secrets/src`.
- Age encryption: `lib/dn/Age/src`.
- SSH keys/protocol: `lib/dn/Ssh/src`.

## Rules

- Do not invent cryptographic algorithms.
- Do not add third-party crypto when `lib/dn/Crypto`, `lib/dn/Age`, or .NET BCL crypto already covers the need.
- Prefer `PasswordHasher` for password hashes.
- Prefer Argon2id when policy allows it.
- Use PBKDF2 only for FIPS or compatibility requirements.
- Use constant-time comparisons for secrets and digests.
- Zero temporary secret buffers when practical.
- Avoid storing secrets in strings when byte/span APIs are practical.
- Never log keys, tokens, passwords, decrypted data, or raw secret material.

## Existing APIs To Check

- `PasswordHasher.Hash(...)` and `PasswordHasher.Verify(...)`.
- `AesGcmEncryptionProvider` for authenticated symmetric encryption.
- `AesCbcEncryptionProvider` only when compatibility requires it.
- `MemoryProtectedBytes` and `MemoryProtectedText` for protected in-memory secret values.
- `EncryptionExtensions.SlowEquals(...)` and BCL `CryptographicOperations.FixedTimeEquals(...)` for comparisons.
- `Csrng` and `RandomNumberGenerator` for randomness.

## Boundary Handling

- Convert crypto failures into `Result<T>` where failure is a normal caller concern.
- Throw only when the method is a low-level primitive whose contract is invalid input equals exception.
- Keep authentication failures distinct from malformed input when callers care.

## Tests

- Test known vectors when available.
- Test round trips.
- Test tampering failure.
- Test wrong-key failure.
- Test malformed input.
- Test memory clearing or constant-time behavior when feasible.
