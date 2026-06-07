using NeoBeard.Exec;
using NeoBeard.Extras;
using NeoBeard.Results;
using NeoBeard.Text;
using NeoBeard.Text.Extras;

using static NeoBeard.Results.Result;

namespace NeoBeard;

/// <summary>
/// Provides environment variable expansion features.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var value = Env.Expand("${HOME}");
/// </code>
/// </example>
/// </remarks>
public static partial class Env
{
    private enum TokenKind
    {
        None,
        Windows,
        BashVariable,
        BashInterpolation,
        CommandSubstitution,
    }

    /// <summary>
    /// Expands environment variables in a template string.
    /// </summary>
    /// <param name="template">
    /// The template string containing environment variables to expand.
    /// </param>
    /// <param name="options">
    /// Options to customize the expansion behavior.
    /// </param>
    /// <returns>
    /// A <see cref="System.ReadOnlySpan{T}"/> representing the expanded template.
    /// </returns>
    /// <exception cref="EnvironmentException">
    /// Thrown when the template contains invalid syntax.
    /// </exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var expanded = Env.Expand("Hello ${USER}");
    /// </code>
    /// </example>
    /// </remarks>
    public static ReadOnlySpan<char> Expand(ReadOnlySpan<char> template, EnvExpandOptions? options = null)
    {
        var o = options ?? new EnvExpandOptions();
        Func<string, string?> getValue = o.GetVariable ?? Environment.GetEnvironmentVariable;
        var setValue = o.SetVariable ?? Environment.SetEnvironmentVariable;
        var tokenBuilder = StringBuilderCache.Acquire();
        var output = StringBuilderCache.Acquire();
        var kind = TokenKind.None;
        var remaining = template.Length;
        for (var i = 0; i < template.Length; i++)
        {
            remaining--;
            var c = template[i];
            if (kind == TokenKind.None)
            {
                if (o.WindowsExpansion && c is '%')
                {
                    kind = TokenKind.Windows;
                    continue;
                }

                var z = i + 1;
                var next = char.MinValue;
                if (z < template.Length)
                    next = template[z];

                // $$ escapes to a single $.
                if (c == '$' && next == '$')
                {
                    output.Append("$");
                    i++;
                    remaining--;
                    continue;
                }

                // escape the $ character.
                if (c is '\\' && next is '$')
                {
                    output.Append(next);
                    i++;
                    continue;
                }

                if (c is '$')
                {
                    if (next is '(')
                    {
                        if (!o.CommandSubstitution)
                            throw new EnvironmentException("Command substitution is not supported.");

                        kind = TokenKind.CommandSubstitution;
                        i++;
                        remaining--;
                        continue;
                    }

                    // can't be a variable if there is no next character.
                    if (next is '{')
                    {
                        if (remaining == 1)
                            throw new EnvironmentException("Bad interpolation. Missing closing token '}'.");

                        if (remaining > 1)
                        {
                            var f = template[z + 1];
                            if (f is '}')
                                throw new EnvironmentException("Bad interpolation. Variable name not provided.");
                        }

                        kind = TokenKind.BashInterpolation;
                        i++;
                        remaining--;
                        continue;
                    }

                    // only a variable if the next character is a letter.
                    if (remaining > 0 && char.IsLetterOrDigit(next))
                    {
                        kind = TokenKind.BashVariable;
                        continue;
                    }

                    throw new EnvironmentException($"Bad interpolation. Variable name not provided.");
                }

                output.Append(c);
                continue;
            }

            if (kind == TokenKind.Windows && c is '%')
            {
                if (tokenBuilder.Length == 0)
                {
                    // consecutive %, so just append both characters.
                    output.Append('%', 2);
                    continue;
                }

                var key = tokenBuilder.ToString();
                var value = getValue(key);
                if (value?.Length > 0)
                    output.Append(value);
                tokenBuilder.Clear();
                kind = TokenKind.None;
                continue;
            }

            if (kind == TokenKind.CommandSubstitution && c is ')')
            {
                if (tokenBuilder.Length == 0)
                {
                    throw new EnvironmentException("Bad substitution. Command not provided.");
                }

                if (o.UseShell.Length == 0)
                {
                    var cmdtokens = CommandLexer.Tokenize(tokenBuilder.ToString());

                    if (cmdtokens.Count == 0)
                    {
                        throw new EnvironmentException("Bad substitution. Command not provided.");
                    }

                    var cmdargs = new CommandArgs();
                    foreach (var t in cmdtokens)
                    {
                        if (t.Kind is CommandTokenKind.DoubleQuotedArg && t.Value.Contains('$'))
                        {
                            cmdargs.Add(Expand(t.Value, o));
                            continue;
                        }

                        if (t.Kind is CommandTokenKind.SingleQuotedArg or CommandTokenKind.Arg or CommandTokenKind.DoubleQuotedArg)
                        {
                            cmdargs.Add(t.Value);
                            continue;
                        }

                        throw new EnvironmentException($"Command substitution does not support token of kind '{t.Kind}'.");
                    }

                    using var proc = new System.Diagnostics.Process();
                    var exe = cmdargs.Shift();
                    proc.StartInfo.FileName = exe;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    foreach (var arg in cmdargs)
                    {
                        proc.StartInfo.ArgumentList.Add(arg);
                    }

                    if (o.GetAllVariables is not null)
                    {
                        foreach (var kvp in o.GetAllVariables())
                        {
                            proc.StartInfo.Environment[kvp.Key] = kvp.Value;
                        }
                    }

                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;

                    proc.Start();
                    var stdout = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();
                    if (proc.ExitCode != 0)
                    {
                        var stderr = proc.StandardError.ReadToEnd();
                        throw new EnvironmentException($"Command substitution failed for command '{tokenBuilder}'. Exit code: {proc.ExitCode}, Error: {stderr}");
                    }

                    if (stdout.Length > 0)
                        output.Append(stdout.TrimEnd());

                    kind = TokenKind.None;
                    tokenBuilder.Clear();
                    continue;
                }
                else
                {
                    var shellArgs = new CommandArgs();
                    switch (o.UseShell.ToLowerInvariant())
                    {
                        case "powershell":
                        case "powershell.exe":
                        case "pwsh":
                        case "pwsh.exe":
                            shellArgs.AddRange(["-NoLogo", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-Command"]);
                            break;
                        case "bash":
                        case "bash.exe":
                            shellArgs.AddRange(["-noprofile", "--norc", "-e", "-o", "pipefail", "-c"]);
                            break;
                        case "sh":
                        case "sh.exe":
                            shellArgs.AddRange(["-e", "-c"]);
                            break;
                    }

                    shellArgs.Add(tokenBuilder.ToString());

                    using var proc = new System.Diagnostics.Process();
                    proc.StartInfo.FileName = o.UseShell;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    foreach (var arg in shellArgs)
                    {
                        proc.StartInfo.ArgumentList.Add(arg);
                    }

                    if (o.GetAllVariables is not null)
                    {
                        foreach (var kvp in o.GetAllVariables())
                        {
                            proc.StartInfo.Environment[kvp.Key] = kvp.Value;
                        }
                    }

                    proc.Start();
                    var stdout = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();
                    if (proc.ExitCode != 0)
                    {
                        var stderr = proc.StandardError.ReadToEnd();
                        throw new EnvironmentException($"Command substitution failed for command '{tokenBuilder}'. Exit code: {proc.ExitCode}, Error: {stderr}");
                    }

                    if (stdout is not null && stdout.Length > 0)
                        output.Append(stdout.TrimEnd());

                    kind = TokenKind.None;
                    tokenBuilder.Clear();
                    continue;
                }
            }

            if (kind == TokenKind.BashInterpolation && c is '}')
            {
                if (tokenBuilder.Length == 0)
                {
                    // with bash '${}' is a bad substitution.
                    throw new EnvironmentException("Bad interpolation. Variable name not provided.");
                }

                var (value, error) = InterpolateVariable(tokenBuilder.AsSpan(), o);
                if (error is not null)
                    throw error;

                output.Append(value);
                tokenBuilder.Clear();
                kind = TokenKind.None;
                continue;
            }

            if (kind == TokenKind.BashVariable && (!(char.IsLetterOrDigit(c) || c is '_') || remaining == 0))
            {
                // '\' is used to escape the next character, so don't append it.
                // its used to escape a name like $HOME\\_TEST where _TEST is not
                // part of the variable name.
                bool append = c is not '\\';

                if (remaining == 0 && (char.IsLetterOrDigit(c) || c is '_'))
                {
                    append = false;
                    tokenBuilder.Append(c);
                }

                // rewind one character. Let the previous block handle $ for the next variable
                if (c is '$')
                {
                    append = false;
                    i--;
                }

                var key = tokenBuilder.ToString();
                if (key.Length == 0)
                {
                    throw new EnvironmentException("Bad interpolation. Variable name not provided.");
                }

                if (o.UnixArgsExpansion && int.TryParse(key, out var index))
                {
                    if (index < 0 || index >= Environment.GetCommandLineArgs().Length)
                        throw new EnvironmentException($"Bad substitution. Invalid args index {index}.");

                    output.Append(Environment.GetCommandLineArgs()[index]);
                    if (append)
                        output.Append(c);

                    tokenBuilder.Clear();
                    kind = TokenKind.None;
                    continue;
                }

                if (!IsValidBashVariable(key.AsSpan()))
                {
                    throw new EnvironmentException($"Bad substitution. Invalid variable name {key}.");
                }

                var value = getValue(key);
                if (value is not null && value.Length > 0)
                    output.Append(value);

                if (value is null)
                    throw new EnvironmentException($"Bad interpolation. Variable {key} is not set.");

                if (append)
                    output.Append(c);

                tokenBuilder.Clear();
                kind = TokenKind.None;
                continue;
            }

            tokenBuilder.Append(c);
            if (remaining == 0)
            {
                if (kind is TokenKind.Windows)
                    throw new EnvironmentException("Bad interpolation. Missing closing token '%'.");

                if (kind is TokenKind.BashInterpolation)
                    throw new EnvironmentException("Bad interpolation. Missing closing token '}'.");
            }
        }

        var set = new char[output.Length];
        output.CopyTo(0, set, 0, output.Length);
        output.Clear();
        return set;
    }

    /// <summary>
    /// Tries to expand environment variables in a template string.
    /// </summary>
    /// <param name="template">
    /// The template string containing environment variables to expand.
    /// </param>
    /// <param name="options">
    /// Options to customize the expansion behavior.
    /// </param>
    /// <returns>
    /// A <see cref="ValueResult{T}"/> representing the outcome of the operation.
    /// </returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = Env.TryExpand("Hello ${USER}".AsSpan());
    /// if (result.IsOk)
    /// {
    ///     var text = result.Value.ToString();
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public static Result<ReadOnlyMemory<char>> TryExpand(ReadOnlySpan<char> template, EnvExpandOptions? options = null)
    {
        var o = options ?? new EnvExpandOptions();
        Func<string, string?> getValue = o.GetVariable ?? Environment.GetEnvironmentVariable;
        var setValue = o.SetVariable ?? Environment.SetEnvironmentVariable;
        var tokenBuilder = StringBuilderCache.Acquire();
        var output = StringBuilderCache.Acquire();
        var kind = TokenKind.None;
        var remaining = template.Length;
        for (var i = 0; i < template.Length; i++)
        {
            remaining--;
            var c = template[i];
            if (kind == TokenKind.None)
            {
                if (o.WindowsExpansion && c is '%')
                {
                    kind = TokenKind.Windows;
                    continue;
                }

                var z = i + 1;
                var next = char.MinValue;
                if (z < template.Length)
                    next = template[z];

                // escape the $ character.
                if (c is '\\' && next is '$')
                {
                    output.Append(next);
                    i++;
                    continue;
                }

                if (c is '$')
                {
                    if (next is '(')
                    {
                        if (!o.CommandSubstitution)
                            return new EnvironmentException("Command substitution is not supported.");

                        kind = TokenKind.CommandSubstitution;
                        i++;
                        remaining--;
                        continue;
                    }

                    // can't be a variable if there is no next character.
                    if (next is '{')
                    {
                        if (remaining == 1)
                            return new EnvironmentException("Bad interpolation. Missing closing token '}'.");

                        if (remaining > 1)
                        {
                            var f = template[z + 1];
                            if (f is '}')
                                return new EnvironmentException("Bad interpolation. Variable name not provided.");
                        }

                        kind = TokenKind.BashInterpolation;
                        i++;
                        remaining--;
                        continue;
                    }

                    // only a variable if the next character is a letter.
                    if (remaining > 0 && char.IsLetterOrDigit(next))
                    {
                        kind = TokenKind.BashVariable;
                        continue;
                    }
                }

                output.Append(c);
                continue;
            }

            if (kind == TokenKind.Windows && c is '%')
            {
                if (tokenBuilder.Length == 0)
                {
                    // consecutive %, so just append both characters.
                    output.Append('%', 2);
                    continue;
                }

                var key = tokenBuilder.ToString();
                var value = getValue(key);
                if (value is not null && value.Length > 0)
                    output.Append(value);
                tokenBuilder.Clear();
                kind = TokenKind.None;
                continue;
            }

            if (kind == TokenKind.CommandSubstitution && c is ')')
            {
                if (tokenBuilder.Length == 0)
                {
                    throw new EnvironmentException("Bad substitution. Command not provided.");
                }

                if (o.UseShell.Length == 0)
                {
                    var cmdtokens = CommandLexer.Tokenize(tokenBuilder.ToString());

                    if (cmdtokens.Count == 0)
                    {
                        throw new EnvironmentException("Bad substitution. Command not provided.");
                    }

                    var cmdargs = new CommandArgs();
                    foreach (var t in cmdtokens)
                    {
                        if (t.Kind is CommandTokenKind.DoubleQuotedArg && t.Value.Contains('$'))
                        {
                            var res = TryExpand(t.Value, o);
                            if (res.HasError)
                            {
                                tokenBuilder.Clear();
                                output.Clear();
                                StringBuilderCache.Release(tokenBuilder);
                                StringBuilderCache.Release(output);
                                return res;
                            }

                            cmdargs.Add(res.ValueOrDefault().ToString());
                            continue;
                        }

                        if (t.Kind is CommandTokenKind.SingleQuotedArg or CommandTokenKind.Arg or CommandTokenKind.DoubleQuotedArg)
                        {
                            cmdargs.Add(t.Value);
                            continue;
                        }

                        return new EnvironmentException($"Command substitution does not support token of kind '{t.Kind}'.");
                    }

                    using var proc = new System.Diagnostics.Process();
                    var exe = cmdargs.Shift();
                    proc.StartInfo.FileName = exe;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    foreach (var arg in cmdargs)
                    {
                        proc.StartInfo.ArgumentList.Add(arg);
                    }

                    if (o.GetAllVariables is not null)
                    {
                        foreach (var kvp in o.GetAllVariables())
                        {
                            proc.StartInfo.Environment[kvp.Key] = kvp.Value;
                        }
                    }

                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;

                    proc.Start();
                    var stdout = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();
                    if (proc.ExitCode != 0)
                    {
                        var stderr = proc.StandardError.ReadToEnd();
                        return new EnvironmentException($"Command substitution failed for command '{tokenBuilder}'. Exit code: {proc.ExitCode}, Error: {stderr}");
                    }

                    if (stdout is not null && stdout.Length > 0)
                        output.Append(stdout.TrimEnd());

                    kind = TokenKind.None;
                    tokenBuilder.Clear();
                    continue;
                }
                else
                {
                    var shellArgs = new CommandArgs();
                    switch (o.UseShell.ToLowerInvariant())
                    {
                        case "powershell":
                        case "powershell.exe":
                        case "pwsh":
                        case "pwsh.exe":
                            shellArgs.AddRange(["-NoLogo", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-Command"]);
                            break;
                        case "bash":
                        case "bash.exe":
                            shellArgs.AddRange(["-noprofile", "--norc", "-e", "-o", "pipefail", "-c"]);
                            break;
                        case "sh":
                        case "sh.exe":
                            shellArgs.AddRange(["-e", "-c"]);
                            break;
                    }

                    shellArgs.Add(tokenBuilder.ToString());

                    using var proc = new System.Diagnostics.Process();
                    proc.StartInfo.FileName = o.UseShell;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    foreach (var arg in shellArgs)
                    {
                        proc.StartInfo.ArgumentList.Add(arg);
                    }

                    if (o.GetAllVariables is not null)
                    {
                        foreach (var kvp in o.GetAllVariables())
                        {
                            proc.StartInfo.Environment[kvp.Key] = kvp.Value;
                        }
                    }

                    proc.Start();
                    var stdout = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();
                    if (proc.ExitCode != 0)
                    {
                        var stderr = proc.StandardError.ReadToEnd();
                        return new EnvironmentException($"Command substitution failed for command '{tokenBuilder}'. Exit code: {proc.ExitCode}, Error: {stderr}");
                    }

                    if (stdout is not null && stdout.Length > 0)
                        output.Append(stdout.TrimEnd());

                    kind = TokenKind.None;
                    tokenBuilder.Clear();
                    continue;
                }
            }

            if (kind == TokenKind.BashInterpolation && c is '}')
            {
                if (tokenBuilder.Length == 0)
                {
                    // with bash '${}' is a bad substitution.
                    return new EnvironmentException("Bad interpolation. Variable name not provided.");
                }

                var (value, defaultValueError) = InterpolateVariable(tokenBuilder.AsSpan(), o);
                if (defaultValueError is not null)
                {
                    tokenBuilder.Clear();
                    return new(defaultValueError);
                }

                output.Append(value);
                tokenBuilder.Clear();
                kind = TokenKind.None;
                continue;
            }

            if (kind == TokenKind.BashVariable && (!(char.IsLetterOrDigit(c) || c is '_') || remaining == 0))
            {
                // '\' is used to escape the next character, so don't append it.
                // its used to escape a name like $HOME\\_TEST where _TEST is not
                // part of the variable name.
                bool append = c is not '\\';

                if (remaining == 0 && (char.IsLetterOrDigit(c) || c is '_'))
                {
                    append = false;
                    tokenBuilder.Append(c);
                }

                // rewind one character. Let the previous block handle $ for the next variable
                if (c is '$')
                {
                    append = false;
                    i--;
                }

                var key = tokenBuilder.ToString();
                if (key.Length == 0)
                {
                    return new(new EnvironmentException("Bad interpolation. Variable name not provided."));
                }

                if (o.UnixArgsExpansion && int.TryParse(key, out var index))
                {
                    if (index < 0 || index >= Environment.GetCommandLineArgs().Length)
                        return new(new EnvironmentException($"Bad interpolation. Invalid args index {index}."));

                    output.Append(Environment.GetCommandLineArgs()[index]);
                    if (append)
                        output.Append(c);

                    tokenBuilder.Clear();
                    kind = TokenKind.None;
                    continue;
                }

                if (!IsValidBashVariable(key.AsSpan()))
                {
                    return new(new EnvironmentException($"Bad interpolation. Invalid variable name {key}."));
                }

                var value = getValue(key);
                if (value is not null && value.Length > 0)
                    output.Append(value);

                if (value is null)
                    return new(new EnvironmentException($"Bad interpolation. Variable {key} is not set."));

                if (append)
                    output.Append(c);

                tokenBuilder.Clear();
                kind = TokenKind.None;
                continue;
            }

            tokenBuilder.Append(c);
            if (remaining == 0)
            {
                if (kind is TokenKind.Windows)
                    return new(new EnvironmentException("Bad interpolation. Missing closing token '%'."));

                if (kind is TokenKind.BashInterpolation)
                    return new(new EnvironmentException("Bad interpolation. Missing closing token '}'."));
            }
        }

        var memory = new Memory<char>(output.ToArray());
        StringBuilderCache.Release(output);
        return (ReadOnlyMemory<char>)memory;
    }

    private static (string, Exception?) InterpolateVariable(ReadOnlySpan<char> token, EnvExpandOptions options)
    {
        var defaultValue = ReadOnlySpan<char>.Empty;
        var message = string.Empty;
        var key = token;

        if (token.Contains(":-", StringComparison.OrdinalIgnoreCase))
        {
            var i = 0;
            foreach (var part in token.Split(":-"))
            {
                if (i == 0)
                {
                    key = token[part];
                    i++;
                    continue;
                }

                defaultValue = token[part];
                break;
            }
        }
        else if (token.Contains(":=", StringComparison.OrdinalIgnoreCase))
        {
            var i = 0;
            foreach (var part in token.Split(":="))
            {
                if (i == 0)
                {
                    key = token[part];
                    i++;
                    continue;
                }

                defaultValue = token[part];
                break;
            }

            if (key.IsEmpty)
            {
                return (string.Empty, new EnvironmentException("Bad interpolation. Variable name not provided."));
            }

            if (!IsValidBashVariable(key))
            {
                return (string.Empty, new EnvironmentException($"Bad interpolation. Invalid variable name {key.ToString()}."));
            }

            options.GetVariable ??= Environment.GetEnvironmentVariable;
            options.SetVariable ??= Environment.SetEnvironmentVariable;

            var v = options.GetVariable(key.ToString());
            if (string.IsNullOrWhiteSpace(v))
            {
                options.SetVariable(key.ToString(), defaultValue.ToString());
            }
        }
        else if (token.Contains(":?", StringComparison.OrdinalIgnoreCase))
        {
            var i = 0;
            foreach (var part in token.Split(":?"))
            {
                if (i == 0)
                {
                    key = token[part];
                    i++;
                    continue;
                }

                message = token[part].ToString();
                break;
            }
        }
        else if (token.Contains(":", StringComparison.OrdinalIgnoreCase))
        {
            var i = 0;
            foreach (var part in token.Split(':'))
            {
                if (i == 0)
                {
                    key = token[part];
                    i++;
                    continue;
                }

                defaultValue = token[part];
                break;
            }
        }

        if (key.IsEmpty)
        {
            return (string.Empty, new EnvironmentException("Bad interpolation. Variable name not provided."));
        }

        if (options.UnixArgsExpansion && int.TryParse(key, out var index))
        {
            if (index < 0 || index >= Environment.GetCommandLineArgs().Length)
                return (string.Empty, new EnvironmentException($"Bad interpolation. Invalid args index {index}."));

            var args = Environment.GetCommandLineArgs();
            if (args.Length > index)
                return (args[index], null);

            return (string.Empty, new EnvironmentException($"Bad interpolation. Invalid args index {index}."));
        }

        if (!IsValidBashVariable(key))
        {
            return (string.Empty, new EnvironmentException($"Bad interpolation. Invalid variable name {key.ToString()}."));
        }

        options.GetVariable ??= Environment.GetEnvironmentVariable;
        var value = options.GetVariable(key.ToString());
        if (string.IsNullOrWhiteSpace(value))
        {
            if (!defaultValue.IsEmpty)
            {
                if (defaultValue.Contains('$'))
                {
                    var res = TryExpand(value.AsSpan(), options);

                    // res is Error e
                    if (res.TryGetError(out var e))
                        return (string.Empty, e);

                    return (res.ValueOrDefault().ToString(), null);
                }

                return (defaultValue.ToString(), null);
            }

            if (message.Length > 0)
            {
                return (string.Empty, new EnvironmentException(message));
            }

            return (string.Empty, null);
        }

        return (value, null);
    }

    private static bool IsValidBashVariable(ReadOnlySpan<char> input)
    {
        for (var i = 0; i < input.Length; i++)
        {
            if (i == 0 && !char.IsLetter(input[i]))
                return false;

            if (!char.IsLetterOrDigit(input[i]) && input[i] is not '_')
                return false;
        }

        return true;
    }
}