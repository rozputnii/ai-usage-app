using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Features.Ledger.Demo;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>T-061: a source change with unchanged data does no redundant view-model work.</summary>
public sealed class UnchangedStateTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    // AC-03: one card property change per update.
    [Fact]
    public async Task ACardRaisesOnePropertyChangePerUpdate()
    {
        var source = new DemoLedgerSource(new ManualScheduler());
        using var window = new LedgerViewModel(source, new ManualScheduler());
        var card = window.Cards.First(c => !c.IsAccountSection);
        var changes = new List<string?>();
        card.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        await source.RefreshAsync(Token);
        Assert.Equal([string.Empty], changes);
    }

    // AC-04: an open cap editor survives an unchanged source change.
    [Fact]
    public async Task AnOpenCapEditorKeepsItsTextAcrossAnUnchangedSourceChange()
    {
        var source = new DemoLedgerSource(new ManualScheduler());
        using var window = new LedgerViewModel(source, new ManualScheduler());
        window.ToggleSettings();
        var row = window.Settings.Caps.First(c => c.CanAct && c.Model.Status != CapStatus.Unmatched);
        await row.ActAsync();
        var editor = Assert.IsType<CapEditorViewModel>(row.Editor);
        editor.Text = "123";
        await source.RefreshAsync(Token);
        Assert.Same(row, window.Settings.Caps.First(c => c.Model.CapId == row.Model.CapId));
        Assert.Same(editor, row.Editor);
        Assert.Equal("123", row.Editor!.Text);
        Assert.True(window.Settings.HasCaps);
    }

    // AC-04: a kept row still follows its card.
    [Fact]
    public void AKeptCapRowFollowsItsCardWhenTheCapsAreEqual()
    {
        var source = new DemoLedgerSource(new ManualScheduler());
        using var window = new LedgerViewModel(source, new ManualScheduler());
        var row = window.Settings.Caps.First(c => c.CanAct && c.Model.Status != CapStatus.Unmatched);
        var target = row.Model.CapTargetId!;
        var changes = new List<string?>();
        row.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        var current = source.Current;
        // The same caps, without the card the row edits.
        var without = current with { Accounts = [.. current.Accounts.Select(a => a with { Cards = [.. a.Cards.Where(c => c.CapTargetId != target)] })] };
        window.Settings.Rebuild(without, source.Preferences);
        Assert.Same(row, window.Settings.Caps.First(c => c.Model.CapId == row.Model.CapId));
        Assert.False(row.CanAct);
        Assert.Contains(nameof(CapRow.CanAct), changes);
        changes.Clear();
        window.Settings.Rebuild(current, source.Preferences);
        Assert.Same(row, window.Settings.Caps.First(c => c.Model.CapId == row.Model.CapId));
        Assert.True(row.CanAct);
        Assert.Contains(nameof(CapRow.CanAct), changes);
    }
}
