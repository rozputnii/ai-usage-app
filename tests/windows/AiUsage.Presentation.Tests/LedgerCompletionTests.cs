using System.Xml.Linq;
using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Features.Ledger.Demo;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class LedgerCompletionTests
{
    [Fact]
    public async Task FailedDemoSignInOffersRetryWithoutAddingAnAccount()
    {
        var scheduler = new ManualScheduler();
        var source = new DemoLedgerSource(scheduler);
        source.LoadScenario(DemoLedgerScenarios.FirstRun);
        using var window = new LedgerViewModel(source, scheduler);
        await window.SignInAsync(ProviderKind.Codex);
        source.FailSignIn();
        scheduler.Run(TimeSpan.FromSeconds(3));
        Assert.Empty(window.Cards);
        Assert.Equal("Codex", window.StripTitle);
        Assert.Equal("sign-in failed", window.StripText);
        Assert.Equal("Try again", window.StripAction);
        scheduler.Run(TimeSpan.FromMinutes(1));
        Assert.True(window.HasStrip);
        await window.StripActionAsync();
        Assert.True(window.StripBusy);
    }

    [Fact]
    public async Task ClosingHistoryReturnsFocusToItsCard()
    {
        var scheduler = new ManualScheduler();
        using var window = new LedgerViewModel(new DemoLedgerSource(scheduler), scheduler);
        var focused = new List<string>();
        window.FocusCardRequested += (_, id) => focused.Add(id);
        await window.Cards[0].ToggleHistoryAsync();
        window.CloseHistory();
        Assert.Equal(["claude-week"], focused);
    }

    [Fact]
    public async Task ReorderingReturnsFocusToTheMovedCardAfterTheOrderChanges()
    {
        var scheduler = new ManualScheduler();
        using var window = new LedgerViewModel(new DemoLedgerSource(scheduler), scheduler);
        var card = window.Cards[0];
        var focused = new List<(string Id, int Position)>();
        window.FocusCardRequested += (_, id) => focused.Add((id, window.Cards.IndexOf(card)));

        await card.MoveDownAsync();
        await card.MoveUpAsync();

        // The Claude account moves past Codex's two cards and back (T-055 R-10).
        Assert.Equal([("claude-week", 2), ("claude-week", 0)], focused);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public async Task EveryGalleryStateHasBoundedGeometryAndSpokenMeaning(bool left, bool compact)
    {
        var scheduler = new ManualScheduler();
        var source = new DemoLedgerSource(scheduler);
        source.LoadScenario(DemoLedgerScenarios.States);
        await source.SetPreferencesAsync(new LedgerPreferences(left ? ValueMode.Left : ValueMode.Used, compact ? Density.Compact : Density.Comfortable, true, false), TestContext.Current.CancellationToken);
        // S3's no-cap state is reached by removing a cap, rather than by a separate reference card.
        await source.SetCapAsync("c1", null, TestContext.Current.CancellationToken);
        using var window = new LedgerViewModel(source, scheduler);
        Assert.Equal(Enum.GetValues<CardState>().Order(), window.Cards.Select(c => c.Model.State).Distinct().Order());
        foreach (var card in window.Cards)
        {
            Assert.Equal(compact, card.IsCompact);
            Assert.NotEmpty(card.Visual.AccessibleName);
            foreach (var segment in card.Visual.Segments)
            {
                Assert.InRange(segment.Left, 0, 100);
                Assert.InRange(segment.Width, 0, 100 - segment.Left + .001);
            }
            foreach (var cell in card.Visual.Cells)
            {
                Assert.True(double.IsFinite(cell.Weight) && cell.Weight > 0);
                Assert.NotEmpty(cell.Tip);
                Assert.All(cell.Parts, p => Assert.True(double.IsFinite(p.Weight) && p.Weight >= 0));
            }
        }
    }

    [Fact]
    public async Task AlwaysOnTopNotifiesTheWindowWhenOnlyThatPreferenceChanges()
    {
        var source = new DemoLedgerSource(new ManualScheduler());
        using var window = new LedgerViewModel(source, new ManualScheduler());
        var changes = new List<string?>();
        window.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        await window.Settings.ToggleAlwaysOnTopAsync();
        Assert.True(window.Preferences.AlwaysOnTop);
        Assert.Contains(nameof(LedgerViewModel.Preferences), changes);
    }

    [Fact]
    public async Task EscapeClosesTheSettingsCapEditorBeforeThePanel()
    {
        var source = new DemoLedgerSource(new ManualScheduler());
        using var window = new LedgerViewModel(source, new ManualScheduler());
        window.ToggleSettings();
        var row = window.Settings.Caps.First();
        await row.ActAsync();
        Assert.NotNull(row.Editor);
        Assert.True(window.Escape());
        Assert.Null(row.Editor);
        Assert.True(window.IsSettingsOpen);
    }

    [Fact]
    public async Task WorkTodayPreservesSignedOutAccountsCardOrderAndEditedCaps()
    {
        var source = new DemoLedgerSource(new ManualScheduler());
        source.LoadScenario(DemoLedgerScenarios.DayOff);
        await source.SignOutAsync("acct-codex", TestContext.Current.CancellationToken);
        await source.MoveCardAsync("claude-extra", -1, TestContext.Current.CancellationToken);
        await source.SetCapAsync("claude-extra", 250, TestContext.Current.CancellationToken);
        await source.SetWorkTodayAsync(true, TestContext.Current.CancellationToken);
        Assert.Equal(AccountHealth.SignedOut, source.Current.Accounts.Single(a => a.AccountId == "acct-codex").Health);
        var claude = source.Current.Accounts.Single(a => a.AccountId == "acct-claude");
        Assert.Equal("claude-extra", claude.Cards[0].CardId);
        Assert.Equal(250, claude.Cards[0].Cap!.Amount);
        await source.SetWorkTodayAsync(false, TestContext.Current.CancellationToken);
        Assert.Equal(250, source.Current.Accounts.SelectMany(a => a.Cards).Single(c => c.CardId == "claude-extra").Cap!.Amount);
    }

    [Fact]
    public async Task DeletingDemoDataInvalidatesUndoAndDayOffState()
    {
        var scheduler = new ManualScheduler();
        var source = new DemoLedgerSource(scheduler);
        source.LoadScenario(DemoLedgerScenarios.DayOff);
        using var window = new LedgerViewModel(source, scheduler);
        await window.ToggleWorkTodayAsync();
        window.Settings.ArmDelete();
        await window.Settings.ConfirmDeleteAsync();
        Assert.False(window.HasUndo);
        await window.UndoAsync();
        Assert.Empty(window.Cards);
        Assert.False(window.IsDayOff);
    }

    [Fact]
    public void EveryClickableControlUsesTheLedgerHandCursorTypesAndStyles()
    {
        var windows = Path.Combine(Repository.Root(), "src/windows/AiUsage.Windows");
        var failures = new List<string>();
        foreach (var path in Directory.EnumerateFiles(windows, "*.xaml", SearchOption.AllDirectories).Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")))
        {
            var doc = XDocument.Load(path);
            foreach (var element in doc.Descendants())
            {
                var name = element.Name.LocalName;
                if (name is "Button" or "CheckBox" or "ToggleSwitch" or "HyperlinkButton" or "ToggleButton")
                    failures.Add($"{Path.GetFileName(path)}: plain {name}");
                if (name is "LedgerButton" or "LedgerCheckBox" && element.Attribute("Style") is null)
                    failures.Add($"{Path.GetFileName(path)}: {name} '{(string?)element.Attribute("Content")}' has no Ledger style");
            }
        }
        foreach (var path in Directory.EnumerateFiles(windows, "*.cs", SearchOption.AllDirectories).Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")))
            foreach (var plain in new[] { "new Button", "new CheckBox" })
                if (File.ReadAllText(path).Contains(plain, StringComparison.Ordinal))
                    failures.Add($"{Path.GetFileName(path)}: {plain}");
        Assert.Empty(failures);
        Assert.Contains("new LedgerClickRow", File.ReadAllText(Path.Combine(windows, "Controls/Ledger/LedgerTrayWindow.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void TextAndStateMarksMeetContrastOnTheirSurfaces()
    {
        var doc = XDocument.Load(Path.Combine(Repository.Root(), "src/windows/AiUsage.Windows/Themes/Ledger/Tokens.xaml"));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var colors = doc.Root!.Elements().Where(e => e.Name.LocalName == "Color")
            .ToDictionary(e => (string)e.Attribute(x + "Key")!, e => Parse(e.Value));
        Color Get(string key) => colors["Ledger" + key + "Color"];
        var failures = new List<string>();
        void Check(string label, Color fg, Color bg, double minimum)
        {
            var a = Luminance(Over(fg, bg));
            var b = Luminance(bg);
            var ratio = Math.Floor((Math.Max(a, b) + .05) / (Math.Min(a, b) + .05) * 100) / 100;
            if (ratio < minimum) failures.Add($"{label}: {ratio:F2} < {minimum}");
        }
        foreach (var surface in new[] { "BgPage", "Card", "CardTop", "CardFreshTop", "Panel", "Hist", "Input", "Tip" })
        {
            foreach (var ink in new[] { "Ink", "Ink2", "Ink3", "OkText", "AttText", "CritText" })
                Check(ink + "/" + surface, Get(ink), Get(surface), 4.5);
            foreach (var mark in new[] { "OkM", "AttM", "CritM", "NeutralM", "Prev", "Focus" })
                Check(mark + "/" + surface, Get(mark), Get(surface), 3);
        }
        foreach (var tone in new[] { "Ok", "Att", "Crit", "Neutral" })
            foreach (var surface in new[] { "Card", "CardTop", "CardFreshTop" })
                Check(tone + " pill/" + surface, Get(tone == "Neutral" ? "Ink2" : tone + "Text"), Over(Get(tone + "Pill"), Get(surface)), 4.5);
        Check("primary button", Get("OnPrimary"), Get("Primary"), 4.5);
        Check("destructive button", Get("Ink"), Get("CritP"), 4.5);
        Check("period-unknown track", Get("NeutralP"), Get("BgPage"), 3);
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private sealed record Color(double R, double G, double B, double A = 1);
    private static Color Parse(string hex)
    {
        var value = Convert.ToUInt32(hex.Trim().TrimStart('#'), 16);
        return new((value >> 16 & 255) / 255.0, (value >> 8 & 255) / 255.0, (value & 255) / 255.0,
            hex.Trim().Length == 9 ? (value >> 24) / 255.0 : 1);
    }
    private static Color Over(Color fg, Color bg) => new(fg.R * fg.A + bg.R * (1 - fg.A), fg.G * fg.A + bg.G * (1 - fg.A), fg.B * fg.A + bg.B * (1 - fg.A));
    private static double Luminance(Color c)
    {
        static double Linear(double v) => v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4);
        return .2126 * Linear(c.R) + .7152 * Linear(c.G) + .0722 * Linear(c.B);
    }
}
