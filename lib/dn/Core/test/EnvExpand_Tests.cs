using NeoBeard;

namespace NeoBeard;

public class EnvExpand_Tests
{
    [Fact]
    public void Expand_ReturnsInput_WhenNoVariables()
    {
        var input = "Hello, world!";
        var result = Env.Expand(input);
        Assert.Equal(input, result.ToString());
    }

    [Fact]
    public void Expand_ExpandsSimpleBashVariable()
    {
        var vars = new Dictionary<string, string> { ["USER"] = "alice" };
        var input = "User: $USER";
        var result = Env.Expand(input, OptionsWithVars(vars));
        Assert.Equal("User: alice", result.ToString());
    }

    [Fact]
    public void Expand_ExpandsBashInterpolation()
    {
        var vars = new Dictionary<string, string> { ["HOME"] = "/home/alice" };
        var input = "Home: ${HOME}";
        var result = Env.Expand(input, OptionsWithVars(vars));
        Assert.Equal("Home: /home/alice", result.ToString());
    }

    [Fact]
    public void Expand_BashVariableWithDefault()
    {
        var vars = new Dictionary<string, string> { ["NAME"] = "Bob" };
        var input = "Name: ${NAME:-DefaultName}";
        var result = Env.Expand(input, OptionsWithVars(vars));
        Assert.Equal("Name: Bob", result.ToString());

        vars = new Dictionary<string, string>();
        input = "Name: ${NAME:-DefaultName}";
        result = Env.Expand(input, OptionsWithVars(vars));
        Assert.Equal("Name: DefaultName", result.ToString());
    }

    [Fact]
    public void Expand_BashVariableAndSetEnv()
    {
        var vars = new Dictionary<string, string>();
        var input = "Path: ${PATH:=/usr/bin}";
        var o1 = OptionsWithVars(vars);
        var result = Env.Expand(input, OptionsWithVars(vars));
        Assert.Equal("Path: /usr/bin", result.ToString());
        Assert.Equal("/usr/bin", vars["PATH"]);

        vars = new Dictionary<string, string> { ["PATH"] = "/bin" };
        input = "Path: ${PATH:=/usr/bin}";
        result = Env.Expand(input, OptionsWithVars(vars));
        Assert.Equal("Path: /bin", result.ToString());
        Assert.Equal("/bin", vars["PATH"]);
    }

    [Fact]
    public void Expand_ThrowsOnMissingBashVariable()
    {
        var input = "User: $USER";
        var ex = Assert.Throws<EnvironmentException>(() => Env.Expand(input, OptionsWithVars(new Dictionary<string, string>())));
        Assert.Contains("variable USER is not set", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Expand_ThrowsOnEmptyBashInterpolation()
    {
        var input = "Value: ${}";
        var ex = Assert.Throws<EnvironmentException>(() => Env.Expand(input, OptionsWithVars(new Dictionary<string, string>())));
        Assert.Contains("Variable name not provided", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Expand_ExpandsWindowsVariable()
    {
        var vars = new Dictionary<string, string> { ["USERNAME"] = "bob" };
        var input = "User: %USERNAME%";
        var options = OptionsWithVars(vars);
        options.WindowsExpansion = true;
        var result = Env.Expand(input, options);
        Assert.Equal("User: bob", result.ToString());
    }

    [Fact]
    public void Expand_ThrowsOnUnclosedWindowsVariable()
    {
        var input = "User: %USERNAME";
        var options = new EnvExpandOptions { WindowsExpansion = true };
        var ex = Assert.Throws<EnvironmentException>(() => Env.Expand(input, options));
        Assert.Contains("missing closing token '%'", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Expand_EscapesDollarSign()
    {
        var input = "Cost: \\$100";
        var result = Env.Expand(input);
        Assert.Equal("Cost: $100", result.ToString());
    }

    [Fact]
    public void Expand_DoubleDollarProducesSingleDollar()
    {
        var input = "PID: $$";
        var result = Env.Expand(input);
        Assert.Equal("PID: $", result.ToString());
    }

    [Fact]
    public void Expand_ExpandsMultipleVariables()
    {
        var vars = new Dictionary<string, string>
        {
            ["FOO"] = "foo",
            ["BAR"] = "bar",
        };
        var input = "$FOO and $BAR";
        var result = Env.Expand(input, OptionsWithVars(vars));
        Assert.Equal("foo and bar", result.ToString());
    }

    [Fact]
    public void Expand_HandlesVariableAtEndOfString()
    {
        var vars = new Dictionary<string, string> { ["END"] = "fin" };
        var input = "The end is $END";
        var result = Env.Expand(input, OptionsWithVars(vars));
        Assert.Equal("The end is fin", result.ToString());
    }

    [Fact]
    public void Expand_ThrowsOnInvalidVariableName()
    {
        var input = "Value: $1INVALID";
        var ex = Assert.Throws<EnvironmentException>(() => Env.Expand(input, OptionsWithVars(new Dictionary<string, string>())));
        Assert.Contains("invalid variable name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Expand_HandlesEscapedVariableName()
    {
        var vars = new Dictionary<string, string> { ["FOO"] = "foo" };
        var input = "$FOO\\_BAR";
        var result = Env.Expand(input, OptionsWithVars(vars));
        Assert.Equal("foo_BAR", result.ToString());
    }

    [Fact]
    public void Expand_ThrowsOnEmptyVariableName()
    {
        var input = "Value: $";
        var ex = Assert.Throws<EnvironmentException>(() => Env.Expand(input, OptionsWithVars(new Dictionary<string, string>())));
        Assert.Contains("Variable name not provided.", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Expand_ThrowsOnUnclosedBashInterpolation()
    {
        var input = "Value: ${FOO";
        var ex = Assert.Throws<EnvironmentException>(() => Env.Expand(input, OptionsWithVars(new Dictionary<string, string>())));
        Assert.Contains("missing closing token '}'", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Expand_CommandSubstitution_IsNotSupported()
    {
        var input = "Today is $(date)";
        var ex = Assert.Throws<EnvironmentException>(() => Env.Expand(input, OptionsWithVars(new Dictionary<string, string>())));
        Assert.Contains("command substitution is not supported", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Expand_CommandSubstitution_WithEcho()
    {
        var echo = "/usr/bin/echo";
        if (!File.Exists(echo))
            Assert.Skip("/usr/bin/echo is not available, cannot run echo command.");

        if (OperatingSystem.IsWindows())
        {
            echo = "C:\\Programs\\Git\\usr\\bin\\echo.exe";
            if (!File.Exists(echo))
                Assert.Skip("Git Bash not installed, cannot run echo command.");
        }

        var input = $"Hello $({echo} \"World $USER\")";
        var options = OptionsWithVars(new Dictionary<string, string> { ["USER"] = "testuser" });
        options.CommandSubstitution = true;

        var result = Env.Expand(input, options);
        Assert.Equal("Hello World testuser", result.ToString());
    }

    /// <summary>
    /// Returns a new EnvExpandOptions with a custom variable dictionary.
    /// </summary>
    private static EnvExpandOptions OptionsWithVars(IDictionary<string, string> vars)
    {
        return new EnvExpandOptions
        {
            GetVariable = k => vars.TryGetValue(k, out var v) ? v : null,
            SetVariable = (k, v) => vars[k] = v,
            GetAllVariables = () => vars,
        };
    }
}