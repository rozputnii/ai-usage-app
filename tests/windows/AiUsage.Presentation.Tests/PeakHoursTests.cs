using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Demo;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>The shared peak hint: weekdays 05:00 to 11:00 Pacific, shown in the owner's local time.</summary>
public sealed class PeakHoursTests
{
    private static readonly TimeSpan Kyiv = TimeSpan.FromHours(3);

    private static DateTimeOffset At(int month, int day, int hour, int minute, TimeSpan offset) => new(2026, month, day, hour, minute, 0, offset);

    [Theory]
    [InlineData(14, 59, false)]
    [InlineData(15, 0, true)]
    [InlineData(20, 59, true)]
    [InlineData(21, 0, false)]
    public void TheWindowStartsAndEndsOnPacificTime(int hour, int minute, bool peak)
    {
        // Friday 9 October 2026: Pacific daylight time (UTC-7), Kyiv summer time (UTC+3).
        Assert.Equal(peak, PeakHours.Window(At(10, 9, hour, minute, Kyiv)) is not null);
    }

    [Fact]
    public void TheWindowIsInTheCallersOffset()
    {
        var (start, end) = PeakHours.Window(At(10, 9, 16, 40, Kyiv))!.Value;
        Assert.Equal(At(10, 9, 15, 0, Kyiv), start);
        Assert.Equal(At(10, 9, 21, 0, Kyiv), end);
        Assert.Equal(Kyiv, end.Offset);
    }

    [Fact]
    public void WeekendsInPacificTimeHaveNoPeak()
    {
        Assert.Null(PeakHours.Window(At(10, 10, 16, 0, Kyiv)));
        Assert.Null(PeakHours.Window(At(10, 11, 16, 0, Kyiv)));
    }

    [Fact]
    public void TheWeekdayIsThePacificOne()
    {
        // Saturday 02:00 in Sydney (UTC+11) is Friday 08:00 in Pacific time; Monday 01:00 in Kyiv is still Sunday there.
        Assert.NotNull(PeakHours.Window(At(10, 10, 2, 0, TimeSpan.FromHours(11))));
        Assert.Null(PeakHours.Window(At(10, 12, 1, 0, Kyiv)));
    }

    [Fact]
    public void DifferentDaylightSavingDatesShiftTheLocalHours()
    {
        // Monday 26 October 2026: Europe is back on winter time (UTC+2), Pacific time is still UTC-7 until 1 November.
        var winter = TimeSpan.FromHours(2);
        var (start, end) = PeakHours.Window(At(10, 26, 15, 0, winter))!.Value;
        Assert.Equal(At(10, 26, 14, 0, winter), start);
        Assert.Equal(At(10, 26, 20, 0, winter), end);
        // Monday 2 November 2026: both on winter time, Pacific UTC-8.
        (start, end) = PeakHours.Window(At(11, 2, 16, 0, winter))!.Value;
        Assert.Equal(At(11, 2, 15, 0, winter), start);
        Assert.Equal(At(11, 2, 21, 0, winter), end);
    }

    [Fact]
    public void TheHintNamesNoProviderAndSaysItIsASchedule()
    {
        var hint = PeakHours.Hint(At(10, 9, 16, 40, Kyiv))!;
        Assert.Equal("Peak · until 21:00", hint.Text);
        Assert.Equal("Peak hours until 21:00", hint.AccessibleName);
        Assert.Equal(["Peak hours", "Weekdays 05:00–11:00 Pacific (15:00–21:00 here)",
            "Requests may be slower or use more of a limit", "An announced schedule, not your account's data"], hint.Tip);
        Assert.Null(PeakHours.Hint(At(10, 9, 22, 0, Kyiv)));
    }

    [Fact]
    public void TheWindowAndTheTrayShowTheHintAndKeepItWhileTheTextIsTheSame()
    {
        var snapshot = DemoLedgerScenarios.Build(DemoLedgerScenarios.Brief);
        var source = new TrayToneTests.SnapshotSource(snapshot with { LocalNow = At(10, 9, 14, 0, Kyiv) });
        using var window = new LedgerViewModel(source, new ManualScheduler());
        using var tray = new LedgerTrayViewModel(source);
        Assert.Null(window.Peak);
        Assert.Null(tray.Peak);

        source.Publish(snapshot with { LocalNow = At(10, 9, 16, 40, Kyiv) });
        var shown = window.Peak;
        Assert.Equal("Peak · until 21:00", shown?.Text);
        Assert.Equal("Peak · until 21:00", tray.Peak?.Text);

        var changes = 0;
        window.PropertyChanged += (_, e) => changes += e.PropertyName == nameof(LedgerViewModel.Peak) ? 1 : 0;
        tray.PropertyChanged += (_, e) => changes += e.PropertyName == nameof(LedgerTrayViewModel.Peak) ? 1 : 0;
        source.Publish(snapshot with { LocalNow = At(10, 9, 16, 41, Kyiv) });
        Assert.Equal(0, changes);
        Assert.Same(shown, window.Peak);

        source.Publish(snapshot with { LocalNow = At(10, 9, 21, 0, Kyiv) });
        Assert.Null(window.Peak);
        Assert.Null(tray.Peak);
    }
}
