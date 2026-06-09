---
name: neo-public-key-encryption
description: User-to-user public-key encryption, Age file encryption, SSH public keys, RSA public keys, Ed25519 SSH keys, recipient/identity handling, and asymmetric encryption design.
---

# Neo Public Key Encryption

Use this skill when users encrypt data for other users or services using public keys.

## Preferred Shape

Use hybrid public-key encryption, not raw RSA encryption of application payloads.

- Generate a random file/content key.
- Encrypt the payload with authenticated symmetric encryption.
- Wrap the content key for each recipient public key.
- Store recipient metadata without exposing private keys.

## Existing Library

- Age implementation: `lib/dn/Age/src`.
- SSH key support: `lib/dn/Age/src/SshRsaKey.cs`, `SshEd25519Key.cs`, `SshRsaAgeRecipient.cs`, `SshEd25519AgeRecipient.cs`.
- Public recipient API: `AgeRecipient.FromPublicKey(...)` and `AgeRecipient.FromSshPublicKey(...)`.
- Private identity API: `AgeIdentity.FromPrivateKey(...)`, `AgeIdentity.FromSshPrivateKey(string pem)`, and `AgeIdentity.FromSshPrivateKey(RSA rsa)`.
- File/stream encryption API: `AgeFile.Encrypt(...)` and `AgeFile.Decrypt(...)`.

## Key Types

- Age X25519 public keys are supported through `AgeRecipient.FromPublicKey(...)`.
- SSH RSA authorized-key lines are supported through `AgeRecipient.FromSshPublicKey(...)`.
- SSH Ed25519 authorized-key lines are supported through `AgeRecipient.FromSshPublicKey(...)`.
- RSA private keys can decrypt through `AgeIdentity.FromSshPrivateKey(...)`.
- OpenSSH private keys can decrypt when parsed by the Age SSH key path.

## Design Rules

- Public keys may be stored and shared.
- Private keys must never be logged, traced, or stored unprotected.
- Prefer recipient lists for multi-user encryption.
- Use key fingerprints for display and audit, not raw private material.
- Include key rotation and revocation considerations in persisted encrypted data.
- Keep decrypted plaintext lifetime short.
- Zero buffers when practical.

## When To Use RSA Directly

- Avoid direct RSA payload encryption.
- If RSA is required, use OAEP and only wrap small keys or envelopes.
- Prefer the Age library path for user-to-user encrypted blobs.

## Error Handling

- Treat no matching identity, malformed key, invalid MAC, wrong key, and tampered payload as normal failure cases for app flows.
- Convert those failures to `Result<T>` at app/service boundaries.
- Do not leak which private keys were tried or why a recipient failed unless needed for safe diagnostics.

## Tests

- Test encrypt/decrypt round trips for age keys.
- Test SSH RSA recipient round trips.
- Test SSH Ed25519 recipient round trips when supported by current parser.
- Test wrong-key and tampered-payload failures.
- Test multi-recipient decrypt where only one identity matches.
