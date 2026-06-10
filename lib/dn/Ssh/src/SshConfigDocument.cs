namespace NeoBeard.Ssh;

/// <summary>
/// Represents a parsed OpenSSH config document with host lookup support.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var doc = SshConfigDocument.Parse("Host *\n  User demo\n");
/// var resolved = doc.Lookup("prod");
/// Console.WriteLine(resolved.User);
/// </code>
/// </example>
/// </remarks>
public sealed class SshConfigDocument
{
    private readonly IReadOnlyList<SshConfigHost> hosts;

    private SshConfigDocument(IReadOnlyList<SshConfigHost> hosts)
    {
        this.hosts = hosts;
    }

    /// <summary>
    /// Gets the parsed host blocks in source order.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IReadOnlyList&lt;SshConfigHost&gt; hosts = doc.Hosts;
    /// </code>
    /// </example>
    /// </remarks>
    public IReadOnlyList<SshConfigHost> Hosts => this.hosts;

    /// <summary>
    /// Parses OpenSSH client config text into a lookup document.
    /// </summary>
    /// <param name="text">The raw OpenSSH config text.</param>
    /// <returns>A parsed config document.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = SshConfigDocument.Parse("Host *\n  User demo\n");
    /// </code>
    /// </example>
    /// </remarks>
    public static SshConfigDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var hosts = new List<MutableHost>();
        var current = new MutableHost(["*"]);
        hosts.Add(current);

        foreach (var rawLine in text.Split(["\r\n", "\n"], StringSplitOptions.None))
        {
            var line = StripComment(rawLine).Trim();
            if (line.Length == 0)
                continue;

            var words = SplitWords(line);
            if (words.Count == 0)
                continue;

            var keyword = words[0].ToLowerInvariant();
            var rest = words.Skip(1).ToArray();
            var value = string.Join(" ", rest);

            if (keyword == "host")
            {
                current = new MutableHost(rest);
                hosts.Add(current);
                continue;
            }

            current.Add(keyword, value);
        }

        return new SshConfigDocument(hosts.Select(ToHost).ToArray());
    }

    /// <summary>
    /// Resolves a host entry using OpenSSH-style first-value and accumulation rules.
    /// </summary>
    /// <param name="host">The target host alias.</param>
    /// <returns>A typed resolved host snapshot.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = SshConfigDocument.Parse("Host *\n  User demo\n");
    /// var resolved = doc.Lookup("prod");
    /// </code>
    /// </example>
    /// </remarks>
    public SshResolvedConfig Lookup(string host)
    {
        ArgumentNullException.ThrowIfNull(host);

        var options = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var entry in this.hosts)
        {
            if (!entry.Patterns.Any(pattern => MatchHostPattern(pattern, host)))
                continue;

            foreach (var kv in entry.Options)
            {
                var key = kv.Key;
                if (IsAccumulative(key))
                {
                    if (!options.TryGetValue(key, out var list))
                    {
                        list = new List<string>();
                        options[key] = list;
                    }

                    list.AddRange(kv.Value.Select(v => ExpandTokens(v, host)));
                }
                else if (!options.ContainsKey(key))
                {
                    options[key] = kv.Value.Select(v => ExpandTokens(v, host)).ToList();
                }
            }
        }

        var flat = options.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<string>)kv.Value,
            StringComparer.Ordinal);

        var sendEnv = SplitPatterns(GetValues(flat, "sendenv"));
        var setEnv = ParseSetEnv(GetValues(flat, "setenv"));

        return new SshResolvedConfig(
            host,
            First(flat, "hostname"),
            First(flat, "user"),
            ParseInt(First(flat, "port")),
            GetValues(flat, "identityfile"),
            sendEnv,
            setEnv,
            ParseBool(First(flat, "identitiesonly")),
            First(flat, "proxycommand"),
            flat);
    }

    private static IReadOnlyList<string> GetValues(IReadOnlyDictionary<string, IReadOnlyList<string>> map, string key)
    {
        return map.TryGetValue(key, out var values) ? values : Array.Empty<string>();
    }

    private static string? First(IReadOnlyDictionary<string, IReadOnlyList<string>> map, string key)
    {
        return map.TryGetValue(key, out var values) ? values.FirstOrDefault() : null;
    }

    private static bool IsAccumulative(string key)
    {
        return key == "identityfile" || key == "sendenv" || key == "setenv";
    }

    private static List<string> SplitPatterns(IReadOnlyList<string> values)
    {
        var outValues = new List<string>();
        foreach (var value in values)
            outValues.AddRange(value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return outValues;
    }

    private static Dictionary<string, string> ParseSetEnv(IReadOnlyList<string> values)
    {
        var env = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in values)
        {
            foreach (var token in SplitWords(line))
            {
                var index = token.IndexOf('=');
                if (index <= 0)
                    continue;

                var key = token[..index];
                var value = token[(index + 1)..];
                env[key] = value;
            }
        }

        return env;
    }

    private static int? ParseInt(string? value)
    {
        if (value is null)
            return null;

        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private static bool? ParseBool(string? value)
    {
        if (value is null)
            return null;

        return value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.OrdinalIgnoreCase);
    }

    private static string StripComment(string line)
    {
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            if (line[i] == '"')
                quoted = !quoted;
            if (!quoted && line[i] == '#')
                return line[..i];
        }

        return line;
    }

    private static List<string> SplitWords(string line)
    {
        var words = new List<string>();
        var regex = new System.Text.RegularExpressions.Regex("\"([^\"]*)\"|'([^']*)'|(\\S+)");
        foreach (System.Text.RegularExpressions.Match match in regex.Matches(line))
        {
            words.Add(match.Groups[1].Value.Length > 0
                ? match.Groups[1].Value
                : match.Groups[2].Value.Length > 0
                ? match.Groups[2].Value
                : match.Groups[3].Value);
        }

        return words;
    }

    private static bool MatchHostPattern(string pattern, string host)
    {
        if (pattern.StartsWith('!'))
            return false;

        var sb = new System.Text.StringBuilder();
        sb.Append('^');
        foreach (var c in pattern)
        {
            switch (c)
            {
                case '*':
                    sb.Append(".*");
                    break;
                case '?':
                    sb.Append('.');
                    break;
                default:
                    if (".+^${}()|[]\\".IndexOf(c, StringComparison.Ordinal) >= 0)
                        sb.Append('\\');
                    sb.Append(c);
                    break;
            }
        }

        sb.Append('$');
        return System.Text.RegularExpressions.Regex.IsMatch(host, sb.ToString(), System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static string ExpandTokens(string value, string host)
    {
        var home = Environment.GetEnvironmentVariable("HOME") ?? "~";
        if (value.StartsWith("~", StringComparison.Ordinal))
            value = home + value[1..];

        return value.Replace("%h", host, StringComparison.Ordinal)
            .Replace("%n", host, StringComparison.Ordinal);
    }

    private static SshConfigHost ToHost(MutableHost host)
    {
        var options = host.Options.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<string>)kv.Value,
            StringComparer.Ordinal);
        return new SshConfigHost(host.Patterns, options);
    }

    private sealed class MutableHost
    {
        public MutableHost(IReadOnlyList<string> patterns)
        {
            this.Patterns = patterns;
        }

        public IReadOnlyList<string> Patterns { get; }

        public Dictionary<string, List<string>> Options { get; } =
            new(StringComparer.Ordinal);

        public void Add(string key, string value)
        {
            if (!this.Options.TryGetValue(key, out var list))
            {
                list = new List<string>();
                this.Options[key] = list;
            }

            list.Add(value);
        }
    }
}