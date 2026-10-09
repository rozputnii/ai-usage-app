using System.Globalization;
using System.Text.RegularExpressions;
using AiUsage.Features.Ledger.Contract;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Ledger;

/// <summary>R-199: the settings popover of one limit. Built from the card's current model; the card rebuilds it after a unit change.</summary>
internal sealed partial class LimitSettingsViewModel : ObservableObject
{
    private const string RateMessage = "Enter a rate above 0, at most 1000, with up to 6 decimals";
    private readonly Func<bool, decimal, Task<bool>> setUnits;
    private readonly Action rebuild;

    public LimitSettingsViewModel(LimitCardModel model, Func<decimal?, Task<bool>> setCap, Func<decimal?, Task<bool>> setToday,
        Func<bool, decimal, Task<bool>> setUnits, Action rebuild, Action close)
    {
        this.setUnits = setUnits;
        this.rebuild = rebuild;
        HasUnits = model.Units is not null;
        IsUsd = model.Units?.Usd == true;
        RateText = model.Units is { } units ? units.Rate.ToString("0.######", CultureInfo.InvariantCulture) : string.Empty;
        var period = LedgerFormat.PeriodWords(model.Period);
        string Fm(decimal value) => LedgerFormat.Value(model.Scale, value);
        if (model.TodayUse is { } today && model.Figures.Used is { } used && model.Figures.DayStart is { } start)
        {
            var tracked = "tracked " + (today.Tracked is { } amount ? Fm(amount) + " " : string.Empty) +
                (today.TrackedSince is { } since ? "since " + LedgerFormat.Clock(since) : "today");
            Today = new CapEditorViewModel(model.Scale, LimitValue.Known(used), used - start, period, setToday, close,
                label: "today", hint: today.Manual ? "set by you · " + tracked + " · empty + Enter returns to it" : tracked + " · Enter saves",
                hasValue: today.Manual, limitNote: "this period’s use", removeText: "Reset", emptyError: "Enter today’s use", failure: "Today’s use was not saved");
        }
        if (model.CapTargetId is not null)
        {
            var before = model.Cap?.Status == CapStatus.CurrencyMismatch ? null : model.Cap?.Amount;
            Cap = new CapEditorViewModel(model.Scale, model.Figures.ProviderLimit, before, period, setCap, close,
                hint: "per " + period + (model.Figures.ProviderLimit is { Kind: LimitValueKind.Known, Amount: { } limit } ? " · at most " + Fm(limit) : string.Empty) +
                    (before is null ? string.Empty : " · empty + Enter removes"));
        }
    }

    public bool HasUnits { get; }
    public bool IsUsd { get; }
    public CapEditorViewModel? Today { get; }
    public CapEditorViewModel? Cap { get; }
    [ObservableProperty] public partial string RateText { get; set; }
    [ObservableProperty] public partial string? RateError { get; set; }

    /// <summary>Typing filter: digits with at most six decimals.</summary>
    public bool AcceptsRate(string text) => Regex.IsMatch(text, @"\A[0-9]*(?:\.[0-9]{0,6})?\z", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    [RelayCommand]
    public Task ShowUsdAsync() => IsUsd ? Task.CompletedTask : ApplyAsync(true);

    [RelayCommand]
    public Task ShowCreditsAsync() => IsUsd ? ApplyAsync(false) : Task.CompletedTask;

    [RelayCommand]
    public Task SaveRateAsync() => ApplyAsync(IsUsd);

    private async Task ApplyAsync(bool usd)
    {
        if (!AcceptsRate(RateText) || !decimal.TryParse(RateText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var rate) || !CreditDollars.ValidRate(rate))
        {
            RateError = RateMessage;
            return;
        }
        RateError = null;
        if (await setUnits(usd, rate))
            rebuild();
        else
            RateError = "The setting was not saved";
    }
}
