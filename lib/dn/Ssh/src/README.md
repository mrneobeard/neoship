# NeoBeard.Ssh

Managed SSH building blocks for .NET.

This module currently provides Go-style session environment planning behavior:

- `SendEnv` pattern selection from process environment variables.
- `SetEnv` fixed key/value environment entries.
- Per-session environment overrides.
- Strict versus permissive env request handling.

## Usage

```csharp
using NeoBeard.Ssh;

var config = new SshClientConfig(
    sendEnv: ["LANG", "LC_*", "APP_*", "-APP_TOKEN"],
    setEnv: new Dictionary<string, string> { ["APP_ENV"] = "prod" },
    strictEnv: false);

var plan = SshEnvironmentPlanner.BuildPlan(
    config,
    new SshSessionOptions(env: new Dictionary<string, string> { ["APP_ENV"] = "override" }));

foreach (var kv in plan.Variables)
{
    Console.WriteLine($"{kv.Key}={kv.Value}");
}
```
