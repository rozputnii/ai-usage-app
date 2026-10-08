using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Features.Ledger.Demo;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>AIU-055 R-11: the refresh interval stepper in Settings, 1 to 60 whole minutes, each change saved at once.</summary>
public sealed class RefreshIntervalSettingsTests
{
    private static (LedgerSettingsViewModel Settings, DemoLedgerSource Source) Start()
    {
        var scheduler = new ManualScheduler();
        var source = new DemoLedgerSource(scheduler);
        source.LoadScenario(DemoLedgerScenarios.Brief);
        return (new LedgerViewModel(source, scheduler).Settings, source);
    }

    private static Task SetSaved(DemoLedgerSource source, int minutes) =>
        source.SetPreferencesAsync(source.Preferences with { RefreshMinutes = minutes }, TestContext.Current.CancellationToken);

    [Fact]
    public async Task StepperSavesWithinOneToSixty()
    {
        var (settings, source) = Start();
        Assert.Equal(5, settings.RefreshMinutes);
        Assert.Equal("5", settings.RefreshText);

        await settings.IncreaseRefreshCommand.ExecuteAsync(null);
        Assert.Equal(6, settings.RefreshMinutes);
        Assert.Equal("6", settings.RefreshText);
        Assert.Equal(6, source.Preferences.RefreshMinutes);
        Assert.Equal(TimeSpan.FromMinutes(6), source.Current.Summaries.RefreshInterval);
        Assert.Equal("Refresh every 6 min · 1 to 60", settings.RefreshTip);

        await SetSaved(source, 60);
        Assert.False(settings.IncreaseRefreshCommand.CanExecute(null));
        Assert.True(settings.DecreaseRefreshCommand.CanExecute(null));
        // Even a command that runs anyway never sends a value above the range.
        await settings.IncreaseRefreshCommand.ExecuteAsync(null);
        Assert.Equal(60, source.Preferences.RefreshMinutes);

        await SetSaved(source, 1);
        Assert.False(settings.DecreaseRefreshCommand.CanExecute(null));
        Assert.True(settings.IncreaseRefreshCommand.CanExecute(null));
        await settings.DecreaseRefreshCommand.ExecuteAsync(null);
        Assert.Equal(1, source.Preferences.RefreshMinutes);
        Assert.Equal("1", settings.RefreshText);
    }

    [Fact]
    public async Task StepperTextEdgeCases()
    {
        var (settings, source) = Start();
        foreach (var typed in new[] { "", "7", "05", "60", "99" })
            Assert.True(settings.AcceptsRefreshText(typed), typed);
        // A paste is judged as a whole; signs, spaces, letters, non-ASCII digits and a third digit are refused.
        foreach (var pasted in new[] { "7a", "a", "-1", " 7", "7 ", "1.5", "123", "٣" })
            Assert.False(settings.AcceptsRefreshText(pasted), pasted);

        foreach (var refused in new[] { "", "0", "61", "a", "7a", "-1", "100", "99999999999" })
        {
            settings.RefreshText = refused;
            Assert.False(await settings.CommitRefreshTextAsync(refused), refused);
            Assert.Equal("5", settings.RefreshText);
            Assert.Equal(5, source.Preferences.RefreshMinutes);
        }

        await SetSaved(source, 7);
        settings.RefreshText = "05";
        Assert.True(await settings.CommitRefreshTextAsync("05"));
        Assert.Equal(5, source.Preferences.RefreshMinutes);
        Assert.Equal("5", settings.RefreshText);

        settings.RefreshText = "15";
        Assert.True(await settings.CommitRefreshTextAsync("15"));
        Assert.Equal(15, source.Preferences.RefreshMinutes);
        Assert.Equal(15, settings.RefreshMinutes);
        Assert.Equal("15", settings.RefreshText);

        // The same value again saves nothing new and leaves the box showing it.
        settings.RefreshText = "15";
        Assert.True(await settings.CommitRefreshTextAsync("15"));
        Assert.Equal(15, source.Preferences.RefreshMinutes);
    }

    [Fact]
    public async Task TypingIsNotOverwrittenByARebuild()
    {
        var (settings, source) = Start();
        settings.RefreshText = "1";
        settings.Rebuild(source.Current, source.Preferences);
        Assert.Equal("1", settings.RefreshText);
        Assert.Equal(5, settings.RefreshMinutes);

        // A saved change from elsewhere does move the box.
        await SetSaved(source, 20);
        Assert.Equal("20", settings.RefreshText);
    }
}
