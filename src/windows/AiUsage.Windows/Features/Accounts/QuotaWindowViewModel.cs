using AiUsage.Features.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiUsage.Features.Accounts;

public enum MeterKind { Bar, Hatch, Unlimited }

public enum ValueTone { Normal, Ok, Warning, Critical, Muted }

/// <summary>
/// One quota window as displayed on Overview lines, detail rows, the hero and the tray. Values come from typed
/// measurements; unknown never renders as an empty zero meter and a passed reset never invents restored quota.
/// </summary>
internal sealed partial class QuotaWindowViewModel : ObservableObject
{
    public QuotaWindowViewModel(string id) => Id = id;

    public string Id { get; }

    [ObservableProperty] public partial string Label { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool IsMuted { get; private set; }
    [ObservableProperty] public partial MeterKind Kind { get; private set; }
    [ObservableProperty] public partial double Fraction { get; private set; }
    [ObservableProperty] public partial ValueTone Tone { get; private set; }
    [ObservableProperty] public partial string Glyph { get; private set; } = string.Empty;
    [ObservableProperty] public partial string ValueText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string UnitText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string StateWord { get; private set; } = string.Empty;
    [ObservableProperty] public partial IReadOnlyList<double> Ticks { get; private set; } = [];
    [ObservableProperty] public partial string ResetRelative { get; private set; } = string.Empty;
    [ObservableProperty] public partial string ResetExact { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool ResetIsCritical { get; private set; }
    [ObservableProperty] public partial bool ResetPassed { get; private set; }
    [ObservableProperty] public partial string AbsoluteText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string AccessibleName { get; private set; } = string.Empty;
    [ObservableProperty] public partial QuotaSeverity Severity { get; private set; }
    /// <summary>Observation revision for this value; meters animate only when it changes together with the value.</summary>
    [ObservableProperty] public partial long Observation { get; private set; }
    [ObservableProperty] public partial double? RemainingPercent { get; private set; }

    /// <summary>Compact bar color: the pace advice where it applies, otherwise <see cref="Tone"/>.</summary>
    [ObservableProperty] public partial ValueTone PaceTone { get; private set; }
    /// <summary>Meter fraction of the end-of-today pace mark, or NaN when there is no daily share.</summary>
    [ObservableProperty] public partial double PaceMark { get; private set; } = double.NaN;
    /// <summary>True when the bar shows remaining quota with a daily share, so the part reserved for later days is faded.</summary>
    [ObservableProperty] public partial bool PaceSplit { get; private set; }
    [ObservableProperty] public partial string PaceText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string BackInText { get; private set; } = string.Empty;
    /// <summary>Hover text of the compact bar: the reading, its reset and the pace advice.</summary>
    [ObservableProperty] public partial string HintText { get; private set; } = string.Empty;

    public bool HasAbsolute => AbsoluteText.Length > 0;
    public bool HasResetExact => ResetExact.Length > 0;
    public bool HasBackIn => BackInText.Length > 0;

    partial void OnAbsoluteTextChanged(string value) => OnPropertyChanged(nameof(HasAbsolute));
    partial void OnResetExactChanged(string value) => OnPropertyChanged(nameof(HasResetExact));
    partial void OnBackInTextChanged(string value) => OnPropertyChanged(nameof(HasBackIn));

    /// <summary>
    /// Compact Overview only (D-181): colors the bar by pace, places the end-of-today mark and writes the hover text. Call
    /// after <see cref="Update"/>. The reading itself, detail views, the tray and notifications keep factual tones.
    /// </summary>
    public void ApplyPace(WindowItem window, Freshness freshness, Preferences preferences, PresentationFormatter format)
    {
        // An exhausted window is already a full critical bar; a pace mark on it would only add noise.
        var advice = Severity == QuotaSeverity.Exhausted ? PaceAdvice.None : QuotaPace.Evaluate(window, freshness, format.Clock.UtcNow, format.Clock.TimeZone);
        var usedMode = preferences.UsageDisplay == UsageDisplay.Used;
        PaceTone = advice.Kind == PaceKind.None ? Tone : advice.Tone;
        PaceMark = advice.MarkPercent is { } mark && Kind == MeterKind.Bar ? (usedMode ? 100 - mark : mark) / 100 : double.NaN;
        PaceSplit = !double.IsNaN(PaceMark) && !usedMode;
        PaceText = advice.Kind switch
        {
            PaceKind.DailyShare when advice.Tone == ValueTone.Critical => -advice.TodayAvailable >= 0.5
                ? format.F("Pace_TodayOver", format.Percent(-advice.TodayAvailable)) : format.T("Pace_TodayUsed"),
            PaceKind.DailyShare when advice.Tone == ValueTone.Warning => advice.TodayAvailable < 0.5
                ? format.T("Pace_SlowDownUnderOne") : format.F("Pace_SlowDown", format.Percent(advice.TodayAvailable)),
            PaceKind.DailyShare => format.F("Pace_CanUse", format.Percent(advice.TodayAvailable)),
            PaceKind.ShortWindow when advice.Tone == ValueTone.Critical => format.F("Pace_ShortLow", format.Percent(QuotaPace.ShortWindowCriticalPercent)),
            _ => string.Empty,
        };
        BackInText = Severity == QuotaSeverity.Exhausted && window.ResetsAt is { } reset
            ? format.Relative(reset) is { } relative ? format.F("Pace_BackIn", relative) : format.T("Reset_PassedAwaiting")
            : string.Empty;
        var value = UnitText.Length > 0 ? $"{ValueText} {UnitText}" : ValueText;
        var reset1 = ResetExact.Length > 0 ? $"{ResetRelative} ({ResetExact})" : ResetRelative;
        HintText = string.Join(Environment.NewLine, new[] { $"{Label} · {value} · {reset1}", PaceText }.Where(line => line.Length > 0));
    }

    public void Update(AccountItem account, GroupItem group, WindowItem window, Preferences preferences, PresentationFormatter format)
    {
        var thresholds = QuotaRules.Resolve(preferences.NotificationRules, account, window).Remaining;
        var severity = QuotaRules.Classify(window, thresholds);
        var usedMode = preferences.UsageDisplay == UsageDisplay.Used;

        Observation = account.ObservationRevision;
        Label = window.Label;
        IsMuted = window.AlertsMuted || preferences.MutedTargets.Contains(window.Id) || preferences.MutedTargets.Contains(group.Id)
            || preferences.MutedTargets.Contains(account.Id);
        Severity = severity;
        RemainingPercent = window.ValueState == ValueState.Exhausted ? 0 : window.ValueState == ValueState.Known ? window.RemainingPercent : null;

        string stateWord;
        if (window.ValueState is ValueState.Known or ValueState.Exhausted && (window.RemainingPercent is not null || window.ValueState == ValueState.Exhausted))
        {
            var remaining = window.ValueState == ValueState.Exhausted ? 0 : Math.Clamp(window.RemainingPercent!.Value, 0, 100);
            var shown = usedMode ? 100 - remaining : remaining;
            Kind = MeterKind.Bar;
            UnitText = format.T(usedMode ? "Value_UnitUsed" : "Value_UnitLeft");
            Ticks = thresholds.Where(t => t is > 0 and < 100).Select(t => (usedMode ? 100 - t : t) / 100.0).ToArray();
            switch (severity)
            {
                case QuotaSeverity.Exhausted:
                    Fraction = 1;
                    ValueText = format.Percent(usedMode ? 100 : 0);
                    Tone = ValueTone.Critical;
                    Glyph = "●";
                    stateWord = format.T("State_Exhausted");
                    break;
                case QuotaSeverity.Critical:
                    Fraction = shown / 100;
                    ValueText = format.Percent(shown);
                    Tone = ValueTone.Critical;
                    Glyph = "●";
                    stateWord = format.T("State_Critical");
                    break;
                case QuotaSeverity.Warning:
                    Fraction = shown / 100;
                    ValueText = format.Percent(shown);
                    Tone = ValueTone.Warning;
                    Glyph = "▲";
                    stateWord = format.T("State_Warning");
                    break;
                default:
                    Fraction = shown / 100;
                    ValueText = format.Percent(shown);
                    Tone = ValueTone.Ok;
                    Glyph = string.Empty;
                    stateWord = format.T("State_Ok");
                    break;
            }
        }
        else
        {
            Fraction = 0;
            Ticks = [];
            UnitText = string.Empty;
            Glyph = string.Empty;
            Tone = ValueTone.Muted;
            (Kind, ValueText, stateWord) = window.ValueState switch
            {
                ValueState.Unlimited => (MeterKind.Unlimited, format.T("Value_Unlimited"), format.T("State_Unlimited")),
                ValueState.Unavailable => (MeterKind.Hatch, format.T("Value_Unavailable"), format.T("State_Unavailable")),
                _ => (MeterKind.Hatch, format.T("Value_Unknown"), format.T("State_Unknown")),
            };
        }
        StateWord = stateWord;

        if (window.ResetsAt is { } reset)
        {
            var relative = format.Relative(reset);
            ResetPassed = relative is null;
            ResetRelative = relative is null ? format.T("Reset_PassedAwaiting") : format.F("Reset_In", relative);
            ResetExact = format.DateTime(reset);
            ResetIsCritical = relative is null && severity == QuotaSeverity.Exhausted;
        }
        else
        {
            ResetPassed = false;
            ResetRelative = format.T(window.ValueState == ValueState.Unlimited ? "Reset_NoneUnlimited" : "Reset_None");
            ResetExact = string.Empty;
            ResetIsCritical = false;
        }

        AbsoluteText = window.Absolute is { } absolute ? AbsoluteDescription(absolute, format) : string.Empty;

        var duration = window.DurationSeconds is { } seconds ? format.F("Aria_Duration", format.Duration(seconds)) : string.Empty;
        var value = UnitText.Length > 0 ? $"{ValueText} {UnitText}" : ValueText;
        var reset1 = ResetExact.Length > 0 ? $"{ResetRelative} · {ResetExact}" : ResetRelative;
        AccessibleName = format.F("Aria_Window", Label, duration, value, StateWord, reset1)
            + (AbsoluteText.Length > 0 ? ". " + AbsoluteText : string.Empty)
            + (IsMuted ? ". " + format.T("Aria_Muted") : string.Empty);
    }

    internal static string AbsoluteDescription(NativeAmount amount, PresentationFormatter format)
    {
        var unit = amount.Unit;
        if (amount.Remaining is not null)
            return amount.Limit is null
                ? format.F("Absolute_RemainingNoLimit", format.NativeNumber(amount.Remaining), unit)
                : format.F("Absolute_RemainingOfLimit", format.NativeNumber(amount.Remaining), format.NativeNumber(amount.Limit), unit);
        if (amount.Used is not null)
            return amount.Limit is null
                ? format.F("Absolute_UsedNoLimit", format.NativeNumber(amount.Used), unit)
                : format.F("Absolute_UsedOfLimit", format.NativeNumber(amount.Used), format.NativeNumber(amount.Limit), unit);
        return format.F("Absolute_NotReported", unit);
    }
}
