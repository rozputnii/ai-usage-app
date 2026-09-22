using System.Text.Json;
using AiUsage.Features.Demo;
using AiUsage.Features.Presentation;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>The C# seeds implement docs/specs/AIU-010-ui-ux/fixtures.json; values that fixtures pin are asserted directly.</summary>
public sealed class ScenarioCatalogTests
{
    private static JsonElement Fixture(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Repository.Root(), "docs/specs/AIU-010-ui-ux/fixtures.json")));
        return document.RootElement.GetProperty("scenarios").EnumerateArray().First(s => s.GetProperty("id").GetString() == id).Clone();
    }

    [Fact]
    public void EveryFixtureScenarioHasASelectableSeed()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Repository.Root(), "docs/specs/AIU-010-ui-ux/fixtures.json")));
        Assert.True(document.RootElement.GetProperty("synthetic").GetBoolean());
        Assert.Equal(DemoScenarioCatalog.T0, DateTimeOffset.Parse(document.RootElement.GetProperty("clock").GetString()!, System.Globalization.CultureInfo.InvariantCulture));
        var fixtureIds = document.RootElement.GetProperty("scenarios").EnumerateArray().Select(s => s.GetProperty("id").GetString()!).ToArray();
        Assert.Equal(15, fixtureIds.Length);
        foreach (var id in fixtureIds)
            Assert.Contains(DemoScenarioCatalog.Scenarios, s => s.FixtureId == id);
        foreach (var scenario in DemoScenarioCatalog.Scenarios)
        {
            var world = DemoScenarioCatalog.Build(scenario.Id);
            Assert.All(world.Accounts, a => Assert.StartsWith("demo-", a.Id));
        }
    }

    [Fact]
    public void F02SeedMatchesFixtureAccountsAndPrimaryValues()
    {
        var world = DemoScenarioCatalog.Build("F02");
        foreach (var expected in Fixture("F02").GetProperty("accounts").EnumerateArray())
        {
            var account = world.Accounts.Single(a => a.Id == expected.GetProperty("id").GetString());
            Assert.Equal(expected.GetProperty("providerId").GetString(), account.ProviderId);
            Assert.Equal(expected.GetProperty("label").GetString(), account.Label);
            var primary = account.AllWindows.First();
            var remaining = expected.GetProperty("remainingPercent");
            if (remaining.ValueKind == JsonValueKind.Null)
                Assert.Null(primary.ToItem().RemainingPercent);
            else
                Assert.Equal(remaining.GetDouble(), primary.ToItem().RemainingPercent);
            if (expected.TryGetProperty("resetsAt", out var reset))
                Assert.Equal(DateTimeOffset.Parse(reset.GetString()!, System.Globalization.CultureInfo.InvariantCulture), primary.ResetsAt);
        }
    }

    [Fact]
    public void F04F06F08F09F11F15SeedsMatchPinnedFixtureValues()
    {
        var f04 = DemoScenarioCatalog.Build("F04").Accounts.Single(a => a.Id == "demo-codex-2");
        Assert.Equal(DateTimeOffset.Parse(Fixture("F04").GetProperty("fetchedAt").GetString()!, System.Globalization.CultureInfo.InvariantCulture), f04.FetchedAt);
        Assert.Equal(Freshness.Stale, f04.Freshness);
        Assert.Equal(Fixture("F04").GetProperty("remainingPercent").GetDouble(), f04.AllWindows.First().Remaining);

        var f06 = DemoScenarioCatalog.Build("F06");
        Assert.Equal(DateTimeOffset.Parse(Fixture("F06").GetProperty("retryAt").GetString()!, System.Globalization.CultureInfo.InvariantCulture), f06.Accounts[1].Failure!.RetryAt);
        Assert.Equal(ConnectionState.ReauthRequired, f06.Accounts[0].Connection);

        var f07 = DemoScenarioCatalog.Build("F07").Accounts.Single(a => a.Id == "demo-claude-1");
        Assert.Equal(Fixture("F07").GetProperty("contexts").EnumerateArray().Select(c => c.GetString()), f07.Contexts.Select(c => c.Id));
        Assert.All(f07.Contexts, c => Assert.Contains(c.Groups, g => g.SharedPoolId == Fixture("F07").GetProperty("sharedPoolId").GetString()));

        var f08 = DemoScenarioCatalog.Build("F08").Accounts.Single(a => a.Id == "demo-antigravity-1").AllWindows.Single();
        Assert.Equal(DateTimeOffset.Parse(Fixture("F08").GetProperty("resetsAt").GetString()!, System.Globalization.CultureInfo.InvariantCulture), f08.ResetsAt);
        Assert.Equal(ValueState.Exhausted, f08.State);
        var money = Fixture("F08").GetProperty("moneyCases").EnumerateArray().ToArray();
        var extensions = DemoScenarioCatalog.Build("F08").Accounts.Single(a => a.Id == "demo-claude-1").Extensions;
        Assert.Equal((money[0].GetProperty("amountMinor").GetString(), 2, "USD"), (extensions[0].AmountMinor, extensions[0].Exponent!.Value, extensions[0].Currency));
        Assert.Equal(("1250", (int?)null, (string?)null), (extensions[1].AmountMinor, extensions[1].Exponent, extensions[1].Currency));

        var points = DemoScenarioCatalog.F09Points(8);
        var expected = Fixture("F09").GetProperty("points").EnumerateArray().ToArray();
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(DateTimeOffset.Parse(expected[i].GetProperty("at").GetString()!, System.Globalization.CultureInfo.InvariantCulture), points[i].At);
            Assert.Equal(expected[i].GetProperty("coverage").GetString(), points[i].Coverage.ToString());
            Assert.Equal(expected[i].GetProperty("segmentId").GetString(), points[i].SegmentId);
            var value = expected[i].GetProperty("remainingPercent");
            Assert.Equal(value.ValueKind == JsonValueKind.Null ? null : value.GetDouble(), points[i].RemainingPercent);
        }

        var f15 = DemoScenarioCatalog.Build("F15");
        var stress = Fixture("F15");
        Assert.Equal(stress.GetProperty("accountCount").GetInt32(), f15.Accounts.Count);
        Assert.All(f15.Accounts, a => Assert.Equal(stress.GetProperty("groupCountPerAccount").GetInt32(), a.Contexts.Single().Groups.Count));
        Assert.Contains(f15.Accounts, a => a.Label == stress.GetProperty("label").GetString());
    }

    [Fact]
    public async Task F11CandidatesMatchFixtureIds()
    {
        using var host = new TestHost();
        var ids = new List<string>();
        await foreach (var candidate in host.Cli.DiscoverAsync(CancellationToken.None))
            ids.Add(candidate.Id);
        Assert.Equal(Fixture("F11").GetProperty("candidates").EnumerateArray().Select(c => c.GetString()), ids);
    }

    [Fact]
    public async Task SeedsAreDeterministicAndResetRestoresTheSeed()
    {
        Assert.Equal(
            JsonSerializer.Serialize(new TestHost().Usage.Current.Accounts),
            JsonSerializer.Serialize(new TestHost().Usage.Current.Accounts));
        using var host = new TestHost();
        var seed = JsonSerializer.Serialize(host.Usage.Current.Accounts);
        await host.Usage.ExecuteAsync(host.Context.Command(UiCommandKind.Rename, "demo-codex-1", new RenamePayload("Changed")), CancellationToken.None);
        host.Controller.AdvanceClock(TimeSpan.FromHours(1));
        Assert.NotEqual(seed, JsonSerializer.Serialize(host.Usage.Current.Accounts));
        await host.Controller.ResetAsync();
        Assert.Equal(seed, JsonSerializer.Serialize(host.Usage.Current.Accounts));
        Assert.Equal(DemoScenarioCatalog.T0, host.Clock.UtcNow);
        Assert.True(host.Usage.Current.Loaded);
        Assert.Equal(UiMode.Demo, host.Usage.Current.Mode);
    }

    [Fact]
    public async Task ScenarioSwitchShowsSkeletonThenNavigatesToItsEntrySurface()
    {
        using var host = new TestHost(autoDelays: false);
        var load = host.Controller.LoadScenarioAsync("F11");
        Assert.False(host.Usage.Current.Loaded);
        Assert.Equal(PageKey.Overview, host.Navigation.Current);
        await host.Delays.Drain();
        await load;
        Assert.True(host.Usage.Current.Loaded);
        Assert.Equal(AiUsage.Features.Presentation.AddAccountTab.ImportFromCli, host.Dialogs.AddAccount.Single().Tab);

        var second = host.Controller.LoadScenarioAsync("F09");
        await host.Delays.Drain();
        await second;
        Assert.Equal(new NavigationRequest(PageKey.History, "demo-claude-1", WindowId: "cl-a-w1"), host.Navigation.Last);
    }
}
