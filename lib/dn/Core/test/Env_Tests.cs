namespace NeoBeard.Tests;

public class Env_Tests
{
    [Fact]
    public void JoinPath_AndSplitPath_RoundTrip()
    {
        var joined = Env.JoinPath("alpha", "beta");

        var split = Env.SplitPath(joined);

        Assert.Equal(2, split.Length);
        Assert.Equal("alpha", split[0]);
        Assert.Equal("beta", split[1]);
    }

    [Fact]
    public void HasPath_StringArray_RespectsPlatformComparison()
    {
        var paths = new[] { "Alpha" };

        var has = Env.HasPath("alpha", paths);

        if (OperatingSystem.IsWindows())
            Assert.True(has);
        else
            Assert.False(has);
    }

    [Fact]
    public void SetAndGet_WorkForProcessEnvironmentVariables()
    {
        var key = "FY_VAR_" + Guid.NewGuid().ToString("N");

        try
        {
            Env.Set(key, "abc");

            Assert.Equal("abc", Env.Get(key));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null, EnvironmentVariableTarget.Process);
        }
    }

    [Fact]
    public void AppendPrependRemovePath_ProcessPath_Works()
    {
        var target = EnvironmentVariableTarget.Process;
        var pathKey = Env.Keys.Path;
        var original = Environment.GetEnvironmentVariable(pathKey, target);

        var first = "fy_first_" + Guid.NewGuid().ToString("N");
        var second = "fy_second_" + Guid.NewGuid().ToString("N");
        var third = "fy_third_" + Guid.NewGuid().ToString("N");

        try
        {
            Env.Set(pathKey, Env.JoinPath(first, second), target);

            Env.PrependPath(third, target);
            var afterPrepend = Env.SplitPath(target);
            Assert.Equal(third, afterPrepend[0]);

            Env.AppendPath(third, target);
            var afterAppend = Env.SplitPath(target);
            Assert.Equal(third, afterAppend[0]);

            Env.RemovePath(third, target);
            var afterRemove = Env.SplitPath(target);
            Assert.DoesNotContain(third, afterRemove);
            Assert.Equal(first, afterRemove[0]);
            Assert.Equal(second, afterRemove[1]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(pathKey, original, target);
        }
    }
}
