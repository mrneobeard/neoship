---
name: neo-perf-systems
description: Performance, low allocations, memory usage, system programming, crypto, SSH, FFI, Span, pooling, and secure memory; use when editing hot paths or lib/dn low-level code.
---

# Neo Perf Systems

Use this skill for low-level and allocation-sensitive code.

## Where It Applies

- `lib/dn/Crypto`.
- `lib/dn/Secrets`.
- `lib/dn/Ssh`.
- `lib/dn/Common/src/ffi`.
- Hot API paths that process tokens, hashes, sessions, or network data.

## Rules

- Optimize only when the code is hot, sensitive, or allocation-heavy.
- Prefer clarity unless performance or memory pressure matters.
- Use `ReadOnlySpan<T>`, `Span<T>`, and `Memory<T>` where they avoid real copies.
- Avoid unnecessary `ToArray()`, `Split()`, string interpolation, and LINQ in hot loops.
- Use `ArrayPool<T>` only with `try/finally` return and clearing when sensitive data is involved.
- Use `stackalloc` only for bounded small buffers.
- Do not return spans to invalid stack or pooled memory.

## Security Memory

- Zero secret buffers after use when practical.
- Use `CryptographicOperations.FixedTimeEquals` for secret comparisons.
- Avoid putting secrets in strings when byte/span alternatives are practical.
- Never log secret material.

## Async And IO

- Avoid blocking waits on async IO.
- Reuse buffers for stream/protocol parsing when safe.
- Preserve existing protocol tests and add edge-case tests for malformed input.

## Validation

- Run targeted low-level library tests after changes.
- Add regression tests for boundary conditions, malformed packets, and secret handling.
