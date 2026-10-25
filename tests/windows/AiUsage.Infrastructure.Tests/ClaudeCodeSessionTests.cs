using System.Text;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers;
using AiUsage.Infrastructure.Providers.Claude;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class ClaudeCodeSessionTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "AiUsage.ClaudeCode.Session.Tests", Guid.NewGuid().ToString("N"));
    private static readonly string[] AuthArguments = ["auth", "status", "--json"];
    private static readonly string[] UsageArguments = ["-p", "/usage", "--output-format", "stream-json", "--verbose", "--no-session-persistence", "--safe-mode"];

    internal static string Auth(bool loggedIn = true, string method = "claude.ai", string email = "owner@example.test",
        string organization = "00000000-0000-4000-8000-000000000001", string plan = "max") =>
        $"{{\"loggedIn\":{(loggedIn ? "true" : "false")},\"authMethod\":\"{method}\",\"apiProvider\":\"firstParty\",\"email\":\"{email}\"," +
        $"\"orgId\":\"{organization}\",\"orgName\":\"Synthetic\",\"subscriptionType\":\"{plan}\"}}";

    /// <summary>Replays queued answers and records every argument list it was given.</summary>
    internal sealed class FakeClaudeCode(params Func<IReadOnlyList<string>, ClaudeCodeRun>[] answers) : IClaudeCode
    {
        private readonly Queue<Func<IReadOnlyList<string>, ClaudeCodeRun>> queue = new(answers);
        public List<string[]> Runs { get; } = [];
        public string? Locate() => @"C:\synthetic\claude.exe";
        public Task<ClaudeCodeRun> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        {
            Runs.Add([.. arguments]);
            return Task.FromResult(queue.Dequeue()(arguments));
        }
        public void Enqueue(params Func<IReadOnlyList<string>, ClaudeCodeRun>[] more) { foreach (var answer in more) queue.Enqueue(answer); }
    }

    internal static Func<IReadOnlyList<string>, ClaudeCodeRun> Answer(string output, int exit = 0) => _ => new(exit, Encoding.UTF8.GetBytes(output));
    internal static Func<IReadOnlyList<string>, ClaudeCodeRun> Fail(ProviderFailureKind kind) => _ => throw new ProviderException(kind);

    private ClaudeCodeSession Session(IClaudeCode claude) => new(claude, new ClaudeCodeStateStore(directory), TimeProvider.System);

    [Fact]
    public async Task ConnectReadsTheSignedInAccountAndItsUsageWithoutABrowserAndKeepsOneProtectedRecord()
    {
        var claude = new FakeClaudeCode(Answer(Auth()), Answer(ClaudeCodeReportTests.Stream()));
        using (var session = Session(claude))
        {
            var connected = await session.ConnectAsync(_ => Assert.Fail("No browser is opened."), TestContext.Current.CancellationToken);
            Assert.Equal(ProviderSessionStatus.QuotaAvailable, connected.Status);
            Assert.Equal("Max", connected.Quota!.PlanType);
            Assert.Equal(3, connected.Quota.Groups.Count);
            Assert.True(session.HasStoredGrant);
            Assert.Equal(new Accounts.ProviderIdentity("owner@example.test", "00000000-0000-4000-8000-000000000001"), session.Identity);
        }
        Assert.Equal([AuthArguments, UsageArguments], claude.Runs);
        Assert.DoesNotContain("owner@example.test", Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(directory, "claude-code.state"))), StringComparison.Ordinal);

        using var resumed = Session(claude);
        var cached = await resumed.ReadCachedStateAsync(TestContext.Current.CancellationToken);
        Assert.True(cached.FromCache);
        Assert.Equal("Max", cached.Quota!.PlanType);
        Assert.Equal(2, claude.Runs.Count);
        Assert.Equal(ProviderSessionStatus.NotConnected, (await resumed.DisconnectAsync(TestContext.Current.CancellationToken)).Status);
        Assert.False(resumed.HasStoredGrant);
        Assert.False(File.Exists(Path.Combine(directory, "claude-code.state")));
        Assert.Equal(2, claude.Runs.Count);
    }

    [Fact]
    public async Task RefreshRereadsUsageForTheSameSignIn()
    {
        var claude = new FakeClaudeCode(Answer(Auth()), Answer(ClaudeCodeReportTests.Stream()), Answer(Auth()), Answer(ClaudeCodeReportTests.Stream()));
        using var session = Session(claude);
        await session.ConnectAsync(_ => { }, TestContext.Current.CancellationToken);
        var refreshed = await session.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ProviderSessionStatus.QuotaAvailable, refreshed.Status);
        Assert.False(refreshed.FromCache);
        Assert.Equal(4, claude.Runs.Count);
    }

    [Theory]
    [InlineData("other@example.test", "00000000-0000-4000-8000-000000000001")]
    [InlineData("owner@example.test", "00000000-0000-4000-8000-000000000002")]
    public async Task ADifferentSignInIsAMismatchThatKeepsTheCachedReadingAndReadsNoUsage(string email, string organization)
    {
        var claude = new FakeClaudeCode(Answer(Auth()), Answer(ClaudeCodeReportTests.Stream()), Answer(Auth(email: email, organization: organization)));
        using var session = Session(claude);
        await session.ConnectAsync(_ => { }, TestContext.Current.CancellationToken);
        var refreshed = await session.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ProviderFailureKind.AccountMismatch, refreshed.Failure);
        Assert.True(refreshed.FromCache);
        Assert.Equal(3, refreshed.Quota!.Groups.Count);
        Assert.Equal(3, claude.Runs.Count);
        Assert.True(session.HasStoredGrant);
    }

    [Theory]
    [InlineData(false, "claude.ai")]
    [InlineData(true, "apiKey")]
    public async Task ConnectWithoutASubscriptionSignInStoresNothing(bool loggedIn, string method)
    {
        var claude = new FakeClaudeCode(Answer(Auth(loggedIn, method), exit: loggedIn ? 0 : 1));
        using var session = Session(claude);
        var state = await session.ConnectAsync(_ => { }, TestContext.Current.CancellationToken);
        Assert.Equal(ProviderFailureKind.AuthenticationRequired, state.Failure);
        Assert.False(session.HasStoredGrant);
        Assert.Single(claude.Runs);
        Assert.False(File.Exists(Path.Combine(directory, "claude-code.state")));
    }

    [Theory]
    [InlineData(ProviderFailureKind.ProviderUnavailable)]
    [InlineData(ProviderFailureKind.Timeout)]
    public async Task ARunnerFailureOnConnectStoresNothing(ProviderFailureKind kind)
    {
        using var session = Session(new FakeClaudeCode(Fail(kind)));
        var state = await session.ConnectAsync(_ => { }, TestContext.Current.CancellationToken);
        Assert.Equal(kind, state.Failure);
        Assert.False(session.HasStoredGrant);
    }

    public static TheoryData<string, int, ProviderFailureKind> RejectedUsage => new()
    {
        { ClaudeCodeReportTests.Stream(), 1, ProviderFailureKind.InvalidResponse },
        { ClaudeCodeReportTests.Stream(cost: 0.18, turns: 1), 0, ProviderFailureKind.InvalidResponse },
        { ClaudeCodeReportTests.Stream(rateLimits: null), 0, ProviderFailureKind.RateLimited },
        { ClaudeCodeReportTests.Stream(report: false), 0, ProviderFailureKind.AuthenticationRequired }
    };

    [Theory]
    [MemberData(nameof(RejectedUsage))]
    public async Task ARejectedUsageRunKeepsTheLastReading(string output, int exit, ProviderFailureKind expected)
    {
        var claude = new FakeClaudeCode(Answer(Auth()), Answer(ClaudeCodeReportTests.Stream()), Answer(Auth()), Answer(output, exit));
        using var session = Session(claude);
        var first = await session.ConnectAsync(_ => { }, TestContext.Current.CancellationToken);
        var refreshed = await session.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Equal(expected, refreshed.Failure);
        Assert.True(refreshed.FromCache);
        Assert.Equal(first.Quota!.FetchedAt, refreshed.Quota!.FetchedAt);
        Assert.True(session.HasStoredGrant);
    }

    [Fact]
    public async Task RefreshWithoutAConnectionRunsNothing()
    {
        var claude = new FakeClaudeCode();
        using var session = Session(claude);
        Assert.Equal(ProviderSessionStatus.NotConnected, (await session.RefreshAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Empty(claude.Runs);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
