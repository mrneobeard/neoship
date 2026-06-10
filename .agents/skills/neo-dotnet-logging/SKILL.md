---
name: neo-dotnet-logging
description: .NET logging, ILogger, Serilog, LoggerMessage source generation, high-performance logging, log sampling, traces, and structured event design; use when adding or reviewing C# logs.
---

# Neo .NET Logging

Use this skill when adding, changing, or reviewing C# logging in this repo.

## Goals

- Logs should help debug closed-app failures without exposing secrets.
- Prefer compile-time/source-generated logging for hot paths, repeated log messages, and non-trivial structured logs.
- Keep log messages structured and stable.
- Avoid unnecessary allocations when a log level is disabled.

## Required References

- High-performance logging: https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/high-performance-logging
- Source-generated logging: https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/source-generation
- Log sampling: https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/log-sampling?tabs=dotnet-cli
- Serilog guidance: https://benfoster.io/blog/serilog-best-practices/

## Level Rules

- `Trace`: key method entry/exit, major branch decisions, correlation points, and pinpointing which closed-app call caused an issue.
- `Debug`: extra diagnostic details useful during active investigation.
- `Information`: durable lifecycle events and state changes operators care about.
- `Warning`: unexpected but handled behavior, policy denial worth operator attention, retries, degraded dependencies.
- `Error`: failed operation requiring investigation, caught exceptions with context.
- `Critical`: app/component cannot safely continue.

For anything below `Warning`, check whether the level is enabled before doing work for the log.

```csharp
var isDebug = this.logger.IsEnabled(LogLevel.Debug);
if (isDebug)
{
    // Build debug-only expensive data here.
    LogResolvedPermissions(this.logger, userId, permissionCount);
}
```

Only check a level once per method:

```csharp
var isTrace = this.logger.IsEnabled(LogLevel.Trace);
var isDebug = this.logger.IsEnabled(LogLevel.Debug);
```

Do not repeatedly call `IsEnabled` through the same method body.

## Source-Generated Logging

Use `LoggerMessage` partial methods when:

- the log appears in a frequently called method.
- the log has structured properties.
- the log has more than trivial string literals.
- the log is below `Warning` and may often be disabled.

Preferred pattern:

```csharp
private static partial class Log
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Trace, Message = "Resolving permissions for user {UserId} in org {OrgId}.")]
    public static partial void ResolvingPermissions(ILogger logger, Guid userId, Guid orgId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Debug, Message = "Resolved {PermissionCount} permissions for user {UserId}.")]
    public static partial void ResolvedPermissions(ILogger logger, Guid userId, int permissionCount);
}
```

Call pattern:

```csharp
var isTrace = this.logger.IsEnabled(LogLevel.Trace);
var isDebug = this.logger.IsEnabled(LogLevel.Debug);

if (isTrace)
{
    Log.ResolvingPermissions(this.logger, userId, orgId);
}

// Work happens here.

if (isDebug)
{
    Log.ResolvedPermissions(this.logger, userId, permissions.Count);
}
```

For `Warning` and higher, logging calls do not need an `IsEnabled` guard unless building log arguments is expensive.

## Structured Logging

- Use message templates with named properties, not interpolation.
- Property names should be stable PascalCase.
- Prefer IDs, stable keys, and safe status values.
- Do not log unbounded collections. Log counts or stable identifiers.

Good:

```csharp
Log.UserSessionRevoked(this.logger, userId, sessionId, reason);
```

Bad:

```csharp
this.logger.LogInformation($"Revoked session {session.Token} for {user.Email}");
```

## Secrets And PII

Never log:

- raw passwords
- API keys
- JWT/JWE bodies
- reset or verification tokens
- passkey challenge responses
- TOTP secrets
- decrypted provider secrets
- OAuth/OIDC authorization codes or access tokens

Prefer digest, suffix, count, id, or stable public key where safe.

Email addresses are PII. Only log email when already part of existing operator-facing behavior and needed. Prefer user id or digest in new logs.

## Trace Placement

Add `Trace` logs to key methods that are hard to diagnose after the fact:

- authentication entry points
- session validation/revocation
- permission resolution
- role/group assignment
- service account authentication
- SSO begin/finish
- passkey begin/finish
- deletion and retention worker flows
- provider/migration selection

Trace should show which call path ran, not dump data.

## Debug Placement

Use `Debug` for extra context:

- counts
- branch decisions
- selected provider name
- cache hit/miss status
- sanitized policy outcome

Guard debug logs and any expensive argument creation with one method-level `isDebug` variable.

## Log Sampling

Use sampling for high-volume repeated logs such as:

- authentication failures by public endpoint
- rate-limit denials
- repeated dependency retry warnings
- polling/background-loop noise

Do not sample:

- security-sensitive mutation audit logs
- startup/migration failure logs
- hard-delete/deletion-retention actions
- rare errors that indicate data loss or corruption

## Audit Vs Logs

Security/business audit events are not normal logs.

- Keep durable security actions in `AuditEvent`.
- Use logs for operator diagnostics around the same operation.
- Do not rely on logs as the source of truth for IAM audit.

## Serilog Notes

- Keep messages semantic, not prose-heavy.
- Avoid dots in property names for broad sink compatibility.
- Prefer contextual properties via middleware/request context over repeating every field manually.
- Keep cardinality controlled. Do not log arbitrary user input as property names or huge values.

## Review Checklist

- Does this log expose secrets or avoidable PII?
- Should this be an audit event instead of or in addition to a log?
- Is the message structured and stable?
- For `Trace`/`Debug`, is `IsEnabled` checked once per method before expensive work?
- Should this use source-generated `LoggerMessage`?
- Is cardinality bounded?
- Is the event level correct?
