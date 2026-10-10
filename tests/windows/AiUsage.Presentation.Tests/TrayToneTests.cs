using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Features.Ledger.Demo;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>
/// T-061 R-01: the tray mark's colour follows the cards of the same source change, and an unchanged change does not ask the
/// window to redraw it.
/// </summary>
public sealed class TrayToneTests
{
    /// <summary>The Brief scenario with every card on track: the same cards, none of them urgent.</summary>
    private static LedgerSnapshot Calm()
    {
        var brief = DemoLedgerScenarios.Build(DemoLedgerScenarios.Brief);
        return brief with
        {
            Accounts = [.. brief.Accounts.Select(a => a with { Cards = [.. a.Cards.Select(c => c with { State = CardState.OnTrack })] })],
        };
    }

    private static bool AnyCritical(LedgerViewModel window) => window.Cards.Any(c => c.Visual.Tone == Tone.Critical);

    [Fact]
    public async Task AnUnchangedSourceChangeLeavesTheTrayToneAlone()
    {
        var source = new SnapshotSource(DemoLedgerScenarios.Build(DemoLedgerScenarios.Brief));
        using var window = new LedgerViewModel(source, new ManualScheduler());
        Assert.Equal(Tone.Critical, window.TrayTone);
        var changes = new List<string?>();
        window.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

        await source.RefreshAsync(TestContext.Current.CancellationToken);
        source.Publish(source.Current);

        Assert.DoesNotContain(nameof(LedgerViewModel.TrayTone), changes);
        Assert.Equal(Tone.Critical, window.TrayTone);
    }

    [Fact]
    public void TheTrayToneTurnsCriticalWithTheCardsAlreadyUpdated()
    {
        var source = new SnapshotSource(Calm());
        using var window = new LedgerViewModel(source, new ManualScheduler());
        Assert.Equal(Tone.Ok, window.TrayTone);
        Assert.False(AnyCritical(window));
        var seen = new List<(Tone Tone, bool CardsCritical)>();
        window.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LedgerViewModel.TrayTone))
                seen.Add((window.TrayTone, AnyCritical(window)));
        };

        source.Publish(DemoLedgerScenarios.Build(DemoLedgerScenarios.Brief));

        Assert.Equal([(Tone.Critical, true)], seen);
    }

    /// <summary>A test source that publishes the snapshots it is given; commands are not used here.</summary>
    private sealed class SnapshotSource(LedgerSnapshot current) : ILedgerSource
    {
        public LedgerSnapshot Current { get; private set; } = current;
        public LedgerPreferences Preferences { get; } = LedgerPreferences.Default;
        public event EventHandler? Changed;

        public void Publish(LedgerSnapshot snapshot)
        {
            Current = snapshot;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public Task RefreshAsync(CancellationToken ct)
        {
            Changed?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public Task SetWorkTodayAsync(bool on, CancellationToken ct) => throw new NotSupportedException();
        public Task<CommandOutcome> RenameAccountAsync(string accountId, string name, CancellationToken ct) => throw new NotSupportedException();
        public Task<CommandOutcome> SetCapAsync(string capTargetId, decimal? amount, CancellationToken ct) => throw new NotSupportedException();
        public Task<CommandOutcome> RemoveUnmatchedCapAsync(string capId, CancellationToken ct) => throw new NotSupportedException();
        public Task<CommandOutcome> SetTodayUsedAsync(string cardId, decimal? amount, CancellationToken ct) => throw new NotSupportedException();
        public Task<CommandOutcome> SetUnitsAsync(string cardId, bool usd, decimal rate, CancellationToken ct) => throw new NotSupportedException();
        public Task<CommandOutcome> SetWorkDaysAsync(IReadOnlySet<DayOfWeek> days, CancellationToken ct) => throw new NotSupportedException();
        public Task MoveCardAsync(string cardId, int offset, CancellationToken ct) => throw new NotSupportedException();
        public Task<CommandOutcome> MoveAccountAsync(string accountId, string? beforeAccountId, CancellationToken ct) => throw new NotSupportedException();
        public Task<CommandOutcome> SetCardHiddenAsync(string cardId, bool hidden, CancellationToken ct) => throw new NotSupportedException();
        public Task SignInAsync(ProviderKind provider, CancellationToken ct) => throw new NotSupportedException();
        public Task ReconnectAsync(string accountId, CancellationToken ct) => throw new NotSupportedException();
        public Task RefreshAccountAsync(string accountId, CancellationToken ct) => throw new NotSupportedException();
        public bool TrySubmitSignInCode(Guid attemptId, string code) => false;
        public Task CancelSignInAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task SignOutAsync(string accountId, CancellationToken ct) => throw new NotSupportedException();
        public Task<CommandOutcome> DeleteStoredDataAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task SetPreferencesAsync(LedgerPreferences preferences, CancellationToken ct) => throw new NotSupportedException();
        public Task<HistoryModel?> GetHistoryAsync(string cardId, CancellationToken ct) => Task.FromResult<HistoryModel?>(null);
    }
}
