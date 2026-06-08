namespace NeoBeard.Ssh;

/// <summary>
/// Builds and applies Go-style SSH session environment request plans.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var config = new SshClientConfig(sendEnv: ["LANG"], strictEnv: false);
/// var plan = SshEnvironmentPlanner.BuildPlan(config, new SshSessionOptions());
/// Console.WriteLine(plan.Variables.Count);
/// </code>
/// </example>
/// </remarks>
public static class SshEnvironmentPlanner
{
    /// <summary>
    /// Builds a session environment plan from client configuration and session overrides.
    /// </summary>
    /// <param name="config">The client configuration.</param>
    /// <param name="options">The session options.</param>
    /// <param name="runtimeEnv">Optional runtime environment source used for SendEnv selection.</param>
    /// <returns>A deterministic session environment plan.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var plan = SshEnvironmentPlanner.BuildPlan(
    ///     new SshClientConfig(sendEnv: ["APP_*"]),
    ///     new SshSessionOptions(),
    ///     new Dictionary&lt;string, string&gt; { ["APP_MODE"] = "prod" });
    /// </code>
    /// </example>
    /// </remarks>
    public static SshSessionEnvPlan BuildPlan(
        SshClientConfig config,
        SshSessionOptions options,
        IReadOnlyDictionary<string, string>? runtimeEnv = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(options);

        var env = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var kv in config.SetEnv)
            env[kv.Key] = kv.Value;

        var source = runtimeEnv ?? LoadRuntimeEnv();
        var selected = SelectSendEnv(source, config.SendEnv);
        foreach (var kv in selected)
            env[kv.Key] = kv.Value;

        foreach (var kv in options.Env)
            env[kv.Key] = kv.Value;

        var strict = options.StrictEnv ?? config.StrictEnv;
        return new SshSessionEnvPlan(env, strict);
    }

    /// <summary>
    /// Applies a session environment plan by sending env requests in plan order.
    /// </summary>
    /// <param name="requester">The session env requester.</param>
    /// <param name="plan">The plan to apply.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when all env requests are processed.</returns>
    /// <exception cref="SshEnvRejectedException">
    /// Thrown when strict mode is enabled and an env request is rejected.
    /// </exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var plan = new SshSessionEnvPlan(new Dictionary&lt;string, string&gt;(), strictEnv: false);
    /// await SshEnvironmentPlanner.ApplyAsync(new Requester(), plan);
    /// </code>
    /// </example>
    /// </remarks>
    public static async Task ApplyAsync(
        ISshSessionEnvRequester requester,
        SshSessionEnvPlan plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requester);
        ArgumentNullException.ThrowIfNull(plan);

        foreach (var kv in plan.Variables)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ok = await requester.SetEnvAsync(kv.Key, kv.Value, cancellationToken).ConfigureAwait(false);
            if (!ok && plan.StrictEnv)
                throw new SshEnvRejectedException(kv.Key);
        }
    }

    /// <summary>
    /// Selects environment variables from a source dictionary using SendEnv patterns.
    /// </summary>
    /// <param name="source">The source environment values.</param>
    /// <param name="patterns">The SendEnv patterns.</param>
    /// <returns>An ordered dictionary of selected variables.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var selected = SshEnvironmentPlanner.SelectSendEnv(
    ///     new Dictionary&lt;string, string&gt; { ["LANG"] = "C.UTF-8" },
    ///     ["LANG"]);
    /// </code>
    /// </example>
    /// </remarks>
    public static IReadOnlyDictionary<string, string> SelectSendEnv(
        IReadOnlyDictionary<string, string> source,
        IReadOnlyList<string> patterns)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(patterns);

        var selected = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var raw in patterns)
        {
            var pattern = (raw ?? string.Empty).Trim();
            if (pattern.Length == 0)
                continue;

            var negated = pattern.StartsWith("-", StringComparison.Ordinal);
            var body = negated ? pattern[1..] : pattern;
            if (body.Length == 0)
                continue;

            foreach (var kv in source)
            {
                if (!SshEnvPatternMatcher.IsMatch(body, kv.Key))
                    continue;

                if (negated)
                    selected.Remove(kv.Key);
                else
                    selected[kv.Key] = kv.Value;
            }
        }

        return selected;
    }

    private static IReadOnlyDictionary<string, string> LoadRuntimeEnv()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        var env = Environment.GetEnvironmentVariables();
        foreach (var key in env.Keys)
        {
            if (key is not string name)
                continue;

            var value = env[key]?.ToString();
            if (value is null)
                continue;

            map[name] = value;
        }

        return map;
    }
}