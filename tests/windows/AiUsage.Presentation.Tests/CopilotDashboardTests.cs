using AiUsage.Core.Dashboard;
using AiUsage.Core.Providers.Copilot;
using AiUsage.Core.Usage;
using AiUsage.Features.Dashboard;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class CopilotDashboardTests
{
    private static readonly DateTimeOffset Now = new(2030, 1, 15, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DeviceCodeIsShownBeforeTheBrowserOpensAndClearedAfterConnection()
    {
        var approve = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new DeviceSession("ABCD-1234", approve.Task, Reading(Report(CopilotUsageReportKind.AiCredits)));
        using var workflow = new DashboardWorkflow(session);
        string? textWhenOpened = null;
        DashboardViewModel? card = null;
        card = new DashboardViewModel(workflow, _ => textWhenOpened = card!.DeviceCodeText, key => key == "CopilotDeviceCodeFormat/Text" ? "Code {0}" : key,
            action => { action(); return Task.CompletedTask; }, "GitHub Copilot", noticeResource: "CopilotNotice/Text");
        Assert.Equal("CopilotNotice/Text", card.NoticeText);
        Assert.False(card.ManualEntryVisible);
        var connect = card.ConnectCommand.ExecuteAsync(null);
        await session.Opened.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Code ABCD-1234", textWhenOpened);
        Assert.Equal("Code ABCD-1234", card.DeviceCodeText);
        approve.SetResult();
        await connect;
        Assert.Equal(string.Empty, card.DeviceCodeText);
        Assert.Equal("CopilotUsageShown/Text", card.StatusText);
        Assert.Contains("CopilotNoUsage/Text", card.ProviderUsageText);
        Assert.Empty(card.Windows);
    }

    [Fact]
    public void UsageTextNeverAddsDifferentUnitsOrInventsAMissingReport()
    {
        static string Resource(string key) => key switch
        {
            "CopilotReportHeaderFormat/Text" => "{0} {1}",
            "CopilotUsageLineFormat/Text" => "{0}|{1}|{2}|{3}|{4}",
            "CopilotReportMissing/Text" => "{0} missing",
            _ => key
        };
        var report = Report(CopilotUsageReportKind.AiCredits,
            Item("ai-credits", 10.5m, 10m, 0.5m, 0.005m), Item("ai-credits", 2m, 0m, 2m, 0.02m), Item("minutes", 7m, 7m, 0m, 0m));
        var text = CopilotUsageText.Format(new(report, null, Now), Resource).Split(Environment.NewLine);
        Assert.Equal("CopilotAiCredits/Text 2030-01", text[0]);
        Assert.Contains(text, line => line.StartsWith("ai-credits|12.5|10|2.5|0.03", StringComparison.Ordinal));
        Assert.Contains(text, line => line.StartsWith("minutes|7|7|0|0", StringComparison.Ordinal));
        Assert.DoesNotContain(text, line => line.Contains("19.5", StringComparison.Ordinal));
        Assert.DoesNotContain(text, line => line.Contains("CopilotPremiumRequests", StringComparison.Ordinal));
        var missing = CopilotUsageText.Format(new(null, Report(CopilotUsageReportKind.PremiumRequests, Item("requests", 3m, 3m, 0m, 0m)), Now), Resource);
        Assert.Contains("CopilotAiCredits/Text missing", missing);
        Assert.Contains("CopilotPremiumRequests/Text 2030-01", missing);
        Assert.Contains("CopilotNoUsage/Text", CopilotUsageText.Format(new(Report(CopilotUsageReportKind.AiCredits), null, Now), Resource));
        Assert.Equal(string.Empty, CopilotUsageText.Format(null, Resource));
    }

    [Fact]
    public async Task CachedUsageIsLabeledAsCached()
    {
        var session = new DeviceSession("unused", Task.CompletedTask, Reading(Report(CopilotUsageReportKind.AiCredits)))
        {
            HasStoredGrant = true,
            Refresh = new(ProviderSessionStatus.QuotaUnavailable, Failure: ProviderFailureKind.ReportUnavailable, RetrievedAt: Now, FromCache: true,
                CopilotUsage: Reading(Report(CopilotUsageReportKind.AiCredits)))
        };
        using var workflow = new DashboardWorkflow(session);
        var card = new DashboardViewModel(workflow, _ => { }, key => key, action => { action(); return Task.CompletedTask; }, "GitHub Copilot");
        await card.RefreshCommand.ExecuteAsync(null);
        Assert.StartsWith("QuotaUnavailable/Text CachedNotice/Text", card.StatusText);
        Assert.Equal("FailureReportUnavailable/Text", card.FailureText);
        Assert.NotEqual(string.Empty, card.ProviderUsageText);
    }

    private static CopilotUsageReading Reading(CopilotUsageReport report) => new(report, null, Now);
    private static CopilotUsageReport Report(CopilotUsageReportKind kind, params CopilotUsageItem[] items) => new(kind, 2030, 1, null, items, Now);
    private static CopilotUsageItem Item(string unit, decimal gross, decimal discount, decimal net, decimal amount) =>
        new("copilot", "synthetic-sku", null, unit, 0.01m, gross, gross / 100, discount, discount / 100, net, amount);

    private sealed class DeviceSession(string code, Task approval, CopilotUsageReading reading) : IProviderSession
    {
        private string? pending;
        public TaskCompletionSource Opened { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool HasStoredGrant { get; set; }
        public ProviderSessionState State { get; private set; } = ProviderSessionState.NotConnected;
        public ProviderSessionState? Refresh { get; init; }
        public string? PendingUserCode => pending;
        public Task<ProviderSessionState> ReadCachedStateAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> ResumeAsync(CancellationToken cancellationToken = default) => RefreshAsync(cancellationToken);
        public Task<ProviderSessionState> RefreshAsync(CancellationToken cancellationToken = default) => Task.FromResult(State = Refresh!);
        public Task<ProviderSessionState> DisconnectAsync(CancellationToken cancellationToken = default) => Task.FromResult(State = ProviderSessionState.NotConnected);
        public async Task<ProviderSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default)
        {
            pending = code;
            openAuthorizationUrl(new Uri("https://github.com/login/device"));
            Opened.SetResult();
            await approval.WaitAsync(cancellationToken);
            pending = null;
            HasStoredGrant = true;
            return State = new(ProviderSessionStatus.QuotaAvailable, RetrievedAt: Now, CopilotUsage: reading);
        }
    }
}
