---
name: neo-results-options
description: NeoBeard Results and Options, Rust-like error flow, avoiding exception-driven .NET flows, Option/ValueOption, Result/ValueResult, and typed errors; use when designing error handling or nullable flows.
---

# Neo Results Options

Use this skill for error and absence modeling in C#.

## Goal

Prefer Rust-like explicit outcomes over common .NET exception bubbling for normal failures.

## APIs

- Namespace: `NeoBeard.Results`.
- Namespace: `NeoBeard.Options`.
- Result files: `lib/dn/Core/src/Results`.
- Option files: `lib/dn/Core/src/Options`.
- Tests: `lib/dn/Core/test/Results` and `lib/dn/Core/test/Options`.

## Result Rules

- Use `Result` for success/failure with no payload.
- Use `Result<T>` for success payload plus standard `Error`.
- Use `Result<T, E>` when the error payload has useful domain shape.
- Use `ValueResult<T, E>` for allocation-sensitive paths or value-like APIs when appropriate.
- Prefer implicit conversions at return sites; the types are designed so returning a value, `Error`, exception, message, `None`, or `Never` can become the correct result or option type.
- Use static helpers such as `Result.Ok(...)`, `Result.Fail(...)`, `Option.Some(...)`, and `Option.None(...)` when they make intent clearer.
- Avoid call-site `new Result(...)`, `new Result<T>(...)`, `new Option<T>(...)`, or `new ValueOption<T>(...)` unless implementing the primitives or tests require constructor coverage.
- Prefer `TryGetValue(out T value)` and `TryGetError(out Error error)` over implicit unwrapping.
- Use `ValueOrDefault(...)` only when a fallback is genuinely safe.
- Avoid `ValueOrThrow()` except at boundaries where throwing is the API contract.

## Option Rules

- Use `Option<T>` when absence is normal and reference-type allocation is acceptable.
- Use `ValueOption<T>` when the option is in a hot path or value-like API.
- Prefer implicit conversions for present values and none markers.
- Use `Option.From(...)`, `Option.Some(...)`, `Option.None(...)`, `ValueOption.Some(...)`, and `ValueOption.None(...)` when they make intent clearer.
- Prefer `TryGetValue(...)` for safe consumption.
- Convert absence to a failure with `ToResult()` when callers need an error path.
- Avoid null checks when the domain can express absence as `Option<T>`.
- Avoid `ValueOrThrow()` except at strict boundary points.

## Preferred Return Style

```csharp
Result<User> FindUser(Guid id)
{
    User? user = LookupUser(id);
    if (user is null)
        return Error.From("User not found.", "user_not_found");

    return user;
}

Option<User> FindCachedUser(Guid id)
{
    User? user = LookupCachedUser(id);
    return Option.From(user);
}
```

- Let implicit conversions do the work.
- Use helpers when they read better.
- Avoid manual constructor noise in application code.

## Exception Rules

- Exceptions are for programming errors, impossible states, framework boundary requirements, and truly exceptional failures.
- Do not throw for normal lookup misses, validation failures, revoked state, expired state, unauthenticated state, or business-rule failure.
- At external boundaries that throw, catch and convert to `Result` near the boundary.
- Preserve the source exception inside `Error` when it is useful for diagnostics.

## API Design

- Return `Result<T>` or `Result<T, E>` from app/library methods that can fail normally.
- Return `Option<T>` or `ValueOption<T>` for lookups where missing is not an error.
- Use clear error codes where callers need branching or API responses.
- Keep error messages safe for logs and responses; do not include secrets.

## Testing

- Test success and failure states.
- Test state transitions that turn values into errors or none.
- Test `TryGetValue`/`TryGetError` behavior for public APIs.
