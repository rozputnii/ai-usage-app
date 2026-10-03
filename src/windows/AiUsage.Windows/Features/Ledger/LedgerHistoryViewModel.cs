using AiUsage.Features.Ledger.Contract;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiUsage.Features.Ledger;

/// <summary>A day bar: Index is its column, Height a fraction of the plot height.</summary>
internal sealed record HistoryBar(int Index, DateOnly Date, decimal Value, double Height, bool IsToday);

/// <summary>A run of days without readings, drawn as a dashed gap, never as zero use.</summary>
internal sealed record HistoryGap(int Start, int Count);

internal sealed record HistoryLabel(int Index, string Text, bool IsToday);

/// <summary>Inline history under a card (spec S4): local readings only, at least 35 days, gaps kept as gaps.</summary>
internal sealed partial class LedgerHistoryViewModel : ObservableObject
{
    private readonly HistoryModel model;
    private readonly ScaleModel scale;

    public LedgerHistoryViewModel(HistoryModel model, string accountName, ScaleModel scale)
    {
        this.model = model;
        this.scale = scale;
        Title = accountName;
        Subtitle = LedgerFormat.PeriodLabel(model.Period) + " · use per local day · last " + model.Days.Count + " days · from this app’s readings";
        var values = model.Days.Where(d => d.Used is not null).Select(d => d.Used!.Value).DefaultIfEmpty(0);
        var top = Math.Max(values.Max() * 1.1m, (model.BaselinePerWorkDay ?? 0) * 1.25m);
        Top = top <= 0 ? 1 : top;
        Bars = [.. model.Days.Select((d, i) => (d, i)).Where(x => x.d.Used is not null)
            .Select(x => new HistoryBar(x.i, x.d.Date, x.d.Used!.Value, x.d.Used.Value <= 0 ? 0 : Math.Max(0.025, (double)(x.d.Used.Value / Top)), x.d.Date == model.Today))];
        var gaps = new List<HistoryGap>();
        for (var i = 0; i < model.Days.Count; i++)
        {
            if (model.Days[i].Used is not null)
                continue;
            var start = i;
            while (i < model.Days.Count && model.Days[i].Used is null)
                i++;
            gaps.Add(new HistoryGap(start, i - start));
        }
        Gaps = gaps;
        ResetTicks = [.. model.Days.Select((d, i) => (d, i)).Where(x => model.ResetDays.Contains(x.d.Date)).Select(x => x.i)];
        Labels = [.. ResetTicks.Where(i => i < model.Days.Count - 5).Select(i => new HistoryLabel(i, LedgerFormat.DayMonth(model.Days[i].Date), false)),
            new HistoryLabel(model.Days.Count - 1, "today", true)];
        Baseline = model.BaselinePerWorkDay is { } b ? (double)(b / Top) : null;
        BaselineText = model.BaselinePerWorkDay is { } baseline ? "baseline " + LedgerFormat.Value(scale, baseline) + " / work day" : string.Empty;
        FocusIndex = model.Days.Count - 1;
    }

    public string CardId => model.CardId;
    public int DayCount => model.Days.Count;
    public string Title { get; }
    public string Subtitle { get; }
    public decimal Top { get; }
    public IReadOnlyList<HistoryBar> Bars { get; }
    public IReadOnlyList<HistoryGap> Gaps { get; }
    public IReadOnlyList<int> ResetTicks { get; }
    public IReadOnlyList<HistoryLabel> Labels { get; }
    public double? Baseline { get; }
    public string BaselineText { get; }
    public string Legend => "Bars: use in each local day. Dashed outline: no readings, a gap that is never drawn as zero. Short ticks: resets. Today is the green bar. History is local only and is kept on sign-out.";
    public string KeysHint => "← → day · Esc closes";

    [ObservableProperty] public partial int FocusIndex { get; private set; }

    public string FocusText
    {
        get
        {
            var day = model.Days[FocusIndex];
            var date = day.Date.ToString("ddd d MMM", System.Globalization.CultureInfo.InvariantCulture);
            return date + " · " + (day.Used is { } used ? LedgerFormat.Value(scale, used) + (scale.Kind == ScaleKind.Percent ? " of " + LedgerFormat.PeriodLabel(model.Period) : string.Empty) : "no readings");
        }
    }

    public string AccessibleName => Title + " history, " + Subtitle.Replace(" · ", ", ", StringComparison.Ordinal) + ". " + LedgerFormat.Spoken(FocusText.Replace(" · ", ", ", StringComparison.Ordinal));

    public void MoveFocus(int delta)
    {
        FocusIndex = Math.Clamp(FocusIndex + delta, 0, model.Days.Count - 1);
        OnPropertyChanged(nameof(FocusText));
        OnPropertyChanged(nameof(AccessibleName));
    }
}
