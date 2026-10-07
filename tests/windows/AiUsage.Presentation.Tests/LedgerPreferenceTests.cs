using AiUsage.Adapters.Live;
using AiUsage.Features.Ledger.Contract;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class LedgerPreferenceTests
{
    [Fact]
    public async Task ImportsOnlyCompatibleGlobalsAndKeepsAccountMetadataSeparate()
    {
        string? saved = null;
        var legacy = """{"Version":1,"Density":0,"UsageDisplay":0,"AlwaysOnTop":true,"ShowDisconnected":true,"Labels":{"claude":"Private old name"},"Order":["claude"]}""";
        var store = new LedgerPreferenceStore(_ => Task.FromResult<string?>(saved),
            (json, _) => { saved = json; return Task.CompletedTask; });
        Assert.True(await store.LoadAsync(legacy, TestContext.Current.CancellationToken));
        Assert.Equal(new(ValueMode.Left, Density.Comfortable, true, true), store.Current.Preferences);
        Assert.Empty(store.Current.Labels);
        Assert.Empty(store.Current.Order);
        var id = Guid.NewGuid().ToString("N");
        Assert.Equal(CommandOutcome.Done, await store.ChangeAsync(s => s with { Labels = new() { [id] = "Work" } }, TestContext.Current.CancellationToken));
        var reopened = new LedgerPreferenceStore(_ => Task.FromResult<string?>(saved), (_, _) => throw new InvalidOperationException());
        Assert.True(await reopened.LoadAsync(null, TestContext.Current.CancellationToken));
        Assert.Equal("Work", reopened.Current.Labels[id]);
        Assert.DoesNotContain("Private old name", saved!);
    }

    [Fact]
    public async Task FilesWithoutUpdateModeLoadAsAlwaysAndModePersists()
    {
        string? saved = """{"Version":1,"Preferences":{"Mode":0,"Density":0,"ShowSignedOut":false,"AlwaysOnTop":false},"Labels":{},"Order":[]}""";
        var store = new LedgerPreferenceStore(_ => Task.FromResult<string?>(saved), (json, _) => { saved = json; return Task.CompletedTask; });
        Assert.True(await store.LoadAsync(null, TestContext.Current.CancellationToken));
        Assert.Equal(UpdateMode.Always, store.Current.Preferences.Updates);
        Assert.Equal(CommandOutcome.Done, await store.ChangeAsync(s => s with { Preferences = s.Preferences with { Updates = UpdateMode.OnLaunch } }, TestContext.Current.CancellationToken));
        var reopened = new LedgerPreferenceStore(_ => Task.FromResult<string?>(saved), (_, _) => Task.CompletedTask);
        Assert.True(await reopened.LoadAsync(null, TestContext.Current.CancellationToken));
        Assert.Equal(UpdateMode.OnLaunch, reopened.Current.Preferences.Updates);
    }

    [Fact]
    public async Task UnknownUpdateModeIsRejectedWithoutOverwrite()
    {
        var writes = 0;
        var original = """{"Version":1,"Preferences":{"Mode":0,"Density":0,"ShowSignedOut":false,"AlwaysOnTop":false,"Updates":7},"Labels":{},"Order":[]}""";
        var store = new LedgerPreferenceStore(_ => Task.FromResult<string?>(original), (_, _) => { writes++; return Task.CompletedTask; });
        Assert.False(await store.LoadAsync(null, TestContext.Current.CancellationToken));
        Assert.Equal(CommandOutcome.Unavailable, await store.ChangeAsync(s => s, TestContext.Current.CancellationToken));
        Assert.Equal(0, writes);
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("{\"Version\":2}")]
    [InlineData("{\"Version\":1,\"Labels\":{\"claude\":\"Old\"}}")]
    public async Task InvalidOrNewerFileIsNeverOverwritten(string original)
    {
        var writes = 0;
        var store = new LedgerPreferenceStore(_ => Task.FromResult<string?>(original), (_, _) => { writes++; return Task.CompletedTask; });
        Assert.False(await store.LoadAsync(null, TestContext.Current.CancellationToken));
        Assert.Equal(CommandOutcome.Unavailable, await store.ChangeAsync(s => s, TestContext.Current.CancellationToken));
        Assert.Equal(0, writes);
    }

    [Fact]
    public async Task ExtensionDataAndDeferredCalendarSurviveReopenAndFailedWrite()
    {
        string? saved = """{"Version":1,"future":{"option":7}}""";
        bool fail = false;
        var store = new LedgerPreferenceStore(_ => Task.FromResult<string?>(saved),
            (json, _) => { if (fail) throw new IOException(); saved = json; return Task.CompletedTask; });
        Assert.True(await store.LoadAsync(null, TestContext.Current.CancellationToken));
        var tomorrow = new DateOnly(2026, 10, 4);
        Assert.Equal(CommandOutcome.Done, await store.ChangeAsync(s => s with
        { PendingWorkDays = [DayOfWeek.Sunday], WorkDaysEffectiveOn = tomorrow, WorkToday = tomorrow.AddDays(-1) }, TestContext.Current.CancellationToken));
        fail = true;
        Assert.Equal(CommandOutcome.Unavailable, await store.ChangeAsync(s => s with { PendingWorkDays = null, WorkDaysEffectiveOn = null }, TestContext.Current.CancellationToken));
        Assert.Equal([DayOfWeek.Sunday], store.Current.PendingWorkDays);
        Assert.Contains("\"future\"", saved!);
        var reopened = new LedgerPreferenceStore(_ => Task.FromResult<string?>(saved), (_, _) => Task.CompletedTask);
        Assert.True(await reopened.LoadAsync(null, TestContext.Current.CancellationToken));
        Assert.Equal(tomorrow, reopened.Current.WorkDaysEffectiveOn);
        await reopened.StopAsync();
        Assert.Equal(CommandOutcome.Unavailable, await reopened.ChangeAsync(s => s, TestContext.Current.CancellationToken));
    }
}
