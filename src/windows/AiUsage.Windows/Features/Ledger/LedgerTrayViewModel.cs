using System.Collections.ObjectModel;
using AiUsage.Features.Ledger.Contract;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiUsage.Features.Ledger;

internal enum TrayStripKind { Cells, EmptyDashed, SolidCritical }

internal enum TrayMark { None, Rush, ExtraUsage }

internal sealed record TrayStrip(string CardId, TrayStripKind Kind, IReadOnlyList<StripCell> Cells, double Opacity, TrayMark Mark,
    IReadOnlyList<string> Tip, IReadOnlyList<string> MarkTip, string AccessibleName);

/// <summary>The five-hour ring (T-055 R-04): the share of the current window used, or left in Left mode, from 0 to 1.</summary>
internal sealed record TrayRing(double Fraction, Tone Tone, IReadOnlyList<string> Tip);

/// <summary>One tray row. <paramref name="Provider"/> picks the mark that replaces the name (R-07). <paramref name="Tip"/> is the
/// mark's tooltip: the display name, then on an error the status lines of <paramref name="NameTip"/>.</summary>
internal sealed record TrayRow(string AccountId, string Name, bool IsError, IReadOnlyList<string> NameTip,
    TrayStrip? Strip, TrayRing? Ring, string AccessibleName, ProviderKind Provider, IReadOnlyList<string> Tip);

/// <summary>
/// The tray flyout as a miniature of the window (R-187, T-055 R-02 to R-07): per account its provider mark, its main limit's
/// one today cell in the value mode and, for a limit with a five-hour window, a ring. No pills, captions, period bars or buttons.
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
    /// <summary>R-06: rows and the title row use the card's padding for the density.</summary>
    [ObservableProperty] public partial bool IsCompact { get; private set; }
    /// <summary>The shared peak hint in the header, or null outside the window; replaced only when its text changes.</summary>
    [ObservableProperty] public partial PeakHint? Peak { get; private set; }
    public string EmptyText => "No accounts yet · open the window to sign in";

    /// <summary>A row click: open the window at that account.</summary>
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
            // R-02: the main limit only; a note-only main limit has no bar.
            var main = AccountCard.Primary(account) is { Layout: not CardLayout.Note } primary ? primary : null;
            var strip = main is null ? null : Strip(main, account, prefs.Mode, snapshot.LocalNow);
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
            var spoken = account.DisplayName + health + ". " + strip?.AccessibleName;
            rows.Add(new TrayRow(account.AccountId, account.DisplayName, error, nameTip, strip, main is null ? null : Ring(main, prefs.Mode), spoken.Trim(),
                account.Provider, [account.DisplayName, .. error ? nameTip : []]));
        }
        return rows;
    }

    private static TrayStrip Strip(LimitCardModel card, AccountModel account, ValueMode mode, DateTimeOffset now)
    {
        var visual = CardVisuals.Build(card, account, mode, now);
        var label = account.DisplayName + " · " + (card.ScopeLabel ?? (card.Layout == CardLayout.FiveHourAndPeriod ? "5h + " + LedgerFormat.PeriodLabel(card.Period) : LedgerFormat.PeriodLabel(card.Period)));
        var kind = card.Layout == CardLayout.UsedOnly ? TrayStripKind.EmptyDashed : card.State == CardState.UsedUp ? TrayStripKind.SolidCritical : TrayStripKind.Cells;
        // R-03: one today cell for every layout; its tip starts with the title "Today", which the label replaces.
        StripCell[] cells = kind == TrayStripKind.Cells ? [CardVisuals.TodayOnlyCell(card, account, mode, now)] : [];
        var tip = cells.Length > 0
            ? [label, .. cells[0].Tip.Skip(1)]
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
        return new TrayStrip(card.CardId, kind, cells, visual.Opacity, mark, tip, markTip, spoken);
    }

    /// <summary>R-04: the current five-hour window of a limit that has one, in the limit's tone.</summary>
    private static TrayRing? Ring(LimitCardModel card, ValueMode mode)
    {
        if (card is not { Layout: CardLayout.FiveHourAndPeriod, FiveHour: { } five })
            return null;
        var tone = card.State == CardState.DayOff ? Tone.Neutral : five.CurrentWindowUsed >= 100 ? Tone.Critical : CardVisuals.ToneOf(card);
        // Before the window starts only the rail shows.
        if (!five.CurrentWindowStarted)
            return new TrayRing(0, tone, ["Next 5h window · starts on first use"]);
        var used = Math.Clamp(five.CurrentWindowUsed, 0, 100);
        var share = mode == ValueMode.Left ? 100 - used : used;
        var until = five.CurrentWindowEndsAt is { } end ? " · until " + LedgerFormat.Clock(end) : string.Empty;
        return new TrayRing((double)(share / 100), tone,
            ["Current 5h window · " + LedgerFormat.Round(share) + (mode == ValueMode.Left ? " % left" : " % used") + until]);
    }

    private void Rebuild()
    {
        IsLeft = source.Preferences.Mode == ValueMode.Left;
        IsCompact = source.Preferences.Density == Density.Compact;
        if (PeakHours.Hint(source.Current.LocalNow) is var peak && peak?.Text != Peak?.Text)
            Peak = peak;
        Rows.Clear();
        foreach (var row in Project(source.Current, source.Preferences))
            Rows.Add(row);
        IsEmpty = Rows.Count == 0;
    }

    public void Dispose() => source.Changed -= OnChanged;
}
