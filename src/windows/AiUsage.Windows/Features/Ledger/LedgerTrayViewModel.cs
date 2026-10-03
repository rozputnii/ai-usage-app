using System.Collections.ObjectModel;
using AiUsage.Features.Ledger.Contract;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiUsage.Features.Ledger;

internal enum TrayStripKind { Cells, EmptyDashed, SolidCritical }

internal enum TrayMark { None, Rush, ExtraUsage }

internal sealed record TrayStrip(string CardId, TrayStripKind Kind, IReadOnlyList<StripCell> Cells, double Opacity, TrayMark Mark,
    IReadOnlyList<string> Tip, IReadOnlyList<string> MarkTip, string AccessibleName);

internal sealed record TrayRow(string AccountId, string Name, bool IsError, IReadOnlyList<string> NameTip, IReadOnlyList<TrayStrip> Strips, string AccessibleName);

/// <summary>
/// The tray flyout as a miniature of the window (D-187, S10 to S10d): per account its name and one today strip per limit in
/// window order and value mode, with no pills, captions, period bars or buttons.
/// </summary>
internal sealed partial class LedgerTrayViewModel : ObservableObject, IDisposable
{
    private readonly ILedgerSource source;

    public LedgerTrayViewModel(ILedgerSource source)
    {
        this.source = source;
        source.Changed += OnChanged;
        Rebuild();
    }

    public ObservableCollection<TrayRow> Rows { get; } = [];
    [ObservableProperty] public partial bool IsLeft { get; private set; }
    [ObservableProperty] public partial bool IsEmpty { get; private set; }
    public string EmptyText => "No accounts yet · open the window to sign in";

    /// <summary>A row click or Enter: open the window at that account.</summary>
    public event EventHandler<string>? OpenAccountRequested;

    public void OpenAccount(string accountId) => OpenAccountRequested?.Invoke(this, accountId);

    private void OnChanged(object? sender, EventArgs e) => Rebuild();

    public static bool IsError(AccountHealth health) =>
        health is AccountHealth.SignInExpired or AccountHealth.SignedOut or AccountHealth.SyncFailedStale or AccountHealth.ProviderError;

    public static IReadOnlyList<TrayRow> Project(LedgerSnapshot snapshot, LedgerPreferences prefs)
    {
        var rows = new List<TrayRow>();
        foreach (var account in snapshot.Accounts.Where(a => prefs.ShowSignedOut || a.Health != AccountHealth.SignedOut))
        {
            var strips = account.Cards.Where(c => c.Layout != CardLayout.Note).Select(c => Strip(c, account, prefs.Mode, snapshot.LocalNow)).ToArray();
            var error = IsError(account.Health);
            var nameTip = account.Health switch
            {
                AccountHealth.SignInExpired => new[] { "Sign-in expired", "Sign in again in the window to refresh" },
                AccountHealth.SignedOut => ["Signed out", "History, name and caps kept"],
                AccountHealth.ProviderError => ["Provider error", "The last reading stays until the provider answers"],
                AccountHealth.SyncFailedStale => [account.LastSyncFailedAt is { } f ? "Sync failed " + LedgerFormat.Clock(f) : "Sync failed",
                    account.LastReadingAt is { } r ? "Showing the reading from " + LedgerFormat.Clock(r) + (account.NextRetryAt is { } n && n > snapshot.LocalNow ? " · retry " + LedgerFormat.Relative(n, snapshot.LocalNow) : string.Empty) : "No current reading"],
                _ => [account.DisplayName],
            };
            var health = error ? ", " + nameTip[0].ToLowerInvariant() + (account.LastReadingAt is { } at ? ", reading " + LedgerFormat.AgeWords(at, snapshot.LocalNow) : string.Empty) : string.Empty;
            var spoken = account.DisplayName + health + ". " + string.Join(" ", strips.Select(s => s.AccessibleName));
            rows.Add(new TrayRow(account.AccountId, account.DisplayName, error, nameTip, strips, spoken.Trim()));
        }
        return rows;
    }

    private static TrayStrip Strip(LimitCardModel card, AccountModel account, ValueMode mode, DateTimeOffset now)
    {
        var visual = CardVisuals.Build(card, account, mode, now);
        var label = account.DisplayName + " · " + (card.ScopeLabel ?? (card.Layout == CardLayout.FiveHourAndPeriod ? "5h + " + LedgerFormat.PeriodLabel(card.Period) : LedgerFormat.PeriodLabel(card.Period)));
        var kind = card.Layout == CardLayout.UsedOnly ? TrayStripKind.EmptyDashed : card.State == CardState.UsedUp ? TrayStripKind.SolidCritical : TrayStripKind.Cells;
        var tip = kind == TrayStripKind.Cells && visual.Cells.Count > 0
            ? [label, .. (mode == ValueMode.Left ? visual.Cells[^1] : visual.Cells[0]).Tip.Skip(1)]
            : new List<string> { label }.Concat(visual.PillTip).ToList();
        var mark = card.State == CardState.Rush ? TrayMark.Rush : card.Marks.Any(m => m.Kind == MarkKind.OnExtraUsage) ? TrayMark.ExtraUsage : TrayMark.None;
        var markTip = mark switch
        {
            TrayMark.Rush => visual.PillTip,
            TrayMark.ExtraUsage => visual.Marks.First(m => m.Kind == MarkKind.OnExtraUsage).Tip,
            _ => [],
        };
        var kindWords = card.Layout switch
        {
            CardLayout.FiveHourAndPeriod => "5 hour and " + LedgerFormat.PeriodWords(card.Period),
            CardLayout.Pool or CardLayout.UsedOnly when card.ScopeLabel is { } scope => scope,
            _ => LedgerFormat.PeriodWords(card.Period),
        };
        var state = visual.Pill is null ? "OK" : LedgerFormat.Spoken(visual.Pill.Replace("5h", "5 hour", StringComparison.Ordinal).Replace("7d", "7 day", StringComparison.Ordinal));
        var detail = tip.Count > 1 ? ", " + LedgerFormat.Spoken(tip[1].Replace(" · ", ", ", StringComparison.Ordinal)) : string.Empty;
        var spoken = char.ToUpperInvariant(kindWords[0]) + kindWords[1..] + ": " + state + detail + (mark == TrayMark.ExtraUsage ? ", on extra usage" : string.Empty) + ".";
        return new TrayStrip(card.CardId, kind, kind == TrayStripKind.Cells ? visual.Cells : [], visual.Opacity, mark, tip, markTip, spoken);
    }

    private void Rebuild()
    {
        IsLeft = source.Preferences.Mode == ValueMode.Left;
        Rows.Clear();
        foreach (var row in Project(source.Current, source.Preferences))
            Rows.Add(row);
        IsEmpty = Rows.Count == 0;
    }

    public void Dispose() => source.Changed -= OnChanged;
}
