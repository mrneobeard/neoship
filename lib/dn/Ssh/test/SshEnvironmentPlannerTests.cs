using System.Threading;

namespace NeoBeard.Ssh.Tests;

public static class SshEnvironmentPlannerTests
{
    [Fact]
    public static void BuildPlan_MergesSetEnvSendEnvAndSessionOverrides()
    {
        var config = new SshClientConfig(
            sendEnv: ["APP_*", "-APP_TOKEN"],
            setEnv: new Dictionary<string, string> { ["APP_ENV"] = "global", ["STATIC"] = "1" },
            strictEnv: false);

        var options = new SshSessionOptions(
            env: new Dictionary<string, string> { ["APP_ENV"] = "override" });

        var runtime = new Dictionary<string, string>
        {
            ["APP_MODE"] = "prod",
            ["APP_TOKEN"] = "redact",
            ["PATH"] = "/usr/bin",
        };

        var plan = SshEnvironmentPlanner.BuildPlan(config, options, runtime);

        Assert.False(plan.StrictEnv);
        Assert.Equal("prod", plan.Variables["APP_MODE"]);
        Assert.Equal("override", plan.Variables["APP_ENV"]);
        Assert.Equal("1", plan.Variables["STATIC"]);
        Assert.False(plan.Variables.ContainsKey("APP_TOKEN"));
        Assert.False(plan.Variables.ContainsKey("PATH"));
    }

    [Fact]
    public static async Task ApplyAsync_StrictModeThrowsOnRejectedEnv()
    {
        var requester = new StubRequester(
            [
                ("LANG", true),
                ("APP_ENV", false),
            ]);

        var plan = new SshSessionEnvPlan(
            new Dictionary<string, string>
            {
                ["LANG"] = "C.UTF-8",
                ["APP_ENV"] = "prod",
            },
            strictEnv: true);

        var ex = await Assert.ThrowsAsync<SshEnvRejectedException>(
            async () => await SshEnvironmentPlanner.ApplyAsync(requester, plan, CancellationToken.None));

        Assert.Equal("APP_ENV", ex.Name);
    }

    [Fact]
    public static async Task ApplyAsync_PermissiveModeContinuesOnRejectedEnv()
    {
        var requester = new StubRequester(
            [
                ("LANG", false),
                ("APP_ENV", true),
            ]);

        var plan = new SshSessionEnvPlan(
            new Dictionary<string, string>
            {
                ["LANG"] = "C.UTF-8",
                ["APP_ENV"] = "prod",
            },
            strictEnv: false);

        await SshEnvironmentPlanner.ApplyAsync(requester, plan, CancellationToken.None);

        Assert.Equal(2, requester.Calls.Count);
        Assert.Equal("LANG", requester.Calls[0]);
        Assert.Equal("APP_ENV", requester.Calls[1]);
    }

    [Fact]
    public static void SelectSendEnv_AppliesWildcardAndNegationInOrder()
    {
        var source = new Dictionary<string, string>
        {
            ["LANG"] = "C.UTF-8",
            ["LC_TIME"] = "en_US.UTF-8",
            ["APP_MODE"] = "prod",
            ["APP_TOKEN"] = "token",
        };

        var selected = SshEnvironmentPlanner.SelectSendEnv(source, ["LANG", "LC_*", "APP_*", "-APP_TOKEN"]);

        Assert.Equal("C.UTF-8", selected["LANG"]);
        Assert.Equal("en_US.UTF-8", selected["LC_TIME"]);
        Assert.Equal("prod", selected["APP_MODE"]);
        Assert.False(selected.ContainsKey("APP_TOKEN"));
    }

    private sealed class StubRequester : ISshSessionEnvRequester
    {
        private readonly IReadOnlyList<(string Name, bool Result)> expected;
        private int index;

        public StubRequester(IReadOnlyList<(string Name, bool Result)> expected)
        {
            this.expected = expected;
        }

        public List<string> Calls { get; } = [];

        public ValueTask<bool> SetEnvAsync(string name, string value, CancellationToken cancellationToken = default)
        {
            this.Calls.Add(name);
            var current = this.expected[this.index];
            this.index++;
            Assert.Equal(current.Name, name);
            return ValueTask.FromResult(current.Result);
        }
    }
}