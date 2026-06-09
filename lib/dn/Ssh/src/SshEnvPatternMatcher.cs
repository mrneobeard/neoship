using System.Text;

namespace NeoBeard.Ssh;

/// <summary>
/// Matches environment variable names against OpenSSH-style SendEnv patterns.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// bool ok = SshEnvPatternMatcher.IsMatch("LC_*", "LC_TIME");
/// Console.WriteLine(ok);
/// </code>
/// </example>
/// </remarks>
public static class SshEnvPatternMatcher
{
    /// <summary>
    /// Determines whether the provided environment key matches an OpenSSH-style pattern.
    /// </summary>
    /// <param name="pattern">The pattern that may contain * and ? wildcards.</param>
    /// <param name="key">The environment key to evaluate.</param>
    /// <returns><c>true</c> if the key matches the pattern; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// bool one = SshEnvPatternMatcher.IsMatch("APP_*", "APP_MODE");
    /// bool two = SshEnvPatternMatcher.IsMatch("APP_*", "PATH");
    /// </code>
    /// </example>
    /// </remarks>
    public static bool IsMatch(string pattern, string key)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(key);

        if (string.Equals(pattern, "*", StringComparison.Ordinal))
            return true;

        var regex = new StringBuilder();
        regex.Append('^');

        foreach (var c in pattern)
        {
            switch (c)
            {
                case '*':
                    regex.Append(".*");
                    break;
                case '?':
                    regex.Append('.');
                    break;
                default:
                    if (".+^${}()|[]\\".IndexOf(c, StringComparison.Ordinal) >= 0)
                        regex.Append('\\');

                    regex.Append(c);
                    break;
            }
        }

        regex.Append('$');
        return System.Text.RegularExpressions.Regex.IsMatch(key, regex.ToString());
    }
}