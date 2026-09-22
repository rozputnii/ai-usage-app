using AiUsage.Infrastructure.Providers;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers.Antigravity;
using System.Text;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class AntigravityQuotaParserTests
{
    private static readonly DateTimeOffset Fetched = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static QuotaSnapshot Parse(string json, string? planType = null) =>
        AntigravityQuotaParser.Parse(Encoding.UTF8.GetBytes(json), Fetched, planType);

    [Fact]
    public void ProviderGroupingWindowsAndResetsArePreservedWithoutModelAttribution()
    {
        var snapshot = Parse(AntigravityProtocolTests.Summary, "free-tier");
        Assert.Equal("free-tier", snapshot.PlanType);
        var group = Assert.Single(snapshot.Groups);
        Assert.Equal("Gemini Models", group.Id);
        Assert.Equal("Gemini Models", group.Name);
        Assert.Null(group.Allowed);
        Assert.Null(group.LimitReached);
        var fiveHour = group.Windows[0];
        Assert.Equal("gemini-5h", fiveHour.Id);
        Assert.Equal(59, fiveHour.RemainingPercent);
        Assert.Equal(41, fiveHour.UsedPercent);
        Assert.Equal(TimeSpan.FromHours(5), fiveHour.Duration);
        Assert.Equal(new DateTimeOffset(2030, 1, 1, 5, 0, 0, TimeSpan.Zero), fiveHour.ResetsAt);
        Assert.Equal("gemini-5h", fiveHour.SourceDetails!.QuotaId);
        Assert.Null(fiveHour.Amount);
        Assert.Null(fiveHour.Unlimited);
        Assert.Equal(TimeSpan.FromDays(7), group.Windows[1].Duration);
        Assert.Equal(88, group.Windows[1].RemainingPercent);
    }

    [Fact]
    public void ASharedThirdPartyGroupIsNeverDuplicatedIntoPerFamilyQuotas()
    {
        var snapshot = Parse("""
            {"groups":[
                {"displayName":"Gemini Models","buckets":[{"bucketId":"gemini-weekly","window":"weekly","remainingFraction":0.5}]},
                {"displayName":"Claude and GPT models","buckets":[{"bucketId":"3p-weekly","window":"weekly","remainingFraction":1}]}]}
            """);
        Assert.Equal(["Gemini Models", "Claude and GPT models"], snapshot.Groups.Select(group => group.Id));
        Assert.All(snapshot.Groups, group => Assert.Single(group.Windows));
    }

    [Fact]
    public void MissingOrOutOfRangeFractionsStayUnknownInsteadOfBecomingZeroOrClamped()
    {
        var snapshot = Parse("""
            {"groups":[{"displayName":"Gemini","buckets":[
                {"bucketId":"no-fraction","window":"weekly","resetTime":"2030-01-08T00:00:00Z"},
                {"bucketId":"negative","remainingFraction":-0.5},
                {"bucketId":"above-one","remainingFraction":1.5},
                {"bucketId":"exhausted","remainingFraction":0}]}]}
            """);
        var windows = snapshot.Groups[0].Windows;
        // A reset time without a fraction is not evidence of exhaustion.
        Assert.Null(windows[0].RemainingPercent);
        Assert.Null(windows[0].UsedPercent);
        Assert.NotNull(windows[0].ResetsAt);
        Assert.Null(windows[1].RemainingPercent);
        Assert.Null(windows[2].RemainingPercent);
        Assert.Equal(0, windows[3].RemainingPercent);
        Assert.Equal(100, windows[3].UsedPercent);
    }

    [Theory]
    [InlineData("""{"bucketId":"credits","remainingAmount":42.5}""", 42.5)]
    [InlineData("""{"bucketId":"credits","remainingAmount":"42.5"}""", 42.5)]
    public void ARemainingAmountKeepsItsUnknownUnitAndIsNeverReadAsAPercentage(string bucket, decimal expected)
    {
        var window = Parse("""{"groups":[{"displayName":"Credits","buckets":[""" + bucket + "]}]}").Groups[0].Windows[0];
        Assert.Equal(new QuotaAmount(expected, null, null, "unknown"), window.Amount);
        Assert.Equal(expected, window.SourceDetails!.QuotaRemaining);
        Assert.Null(window.RemainingPercent);
    }

    [Fact]
    public void DisabledBucketsAreDroppedAndAFullyDisabledGroupIsReportedAsNotAllowed()
    {
        var snapshot = Parse("""
            {"groups":[
                {"displayName":"Partly","buckets":[{"bucketId":"on","remainingFraction":0.5},{"bucketId":"off","disabled":true,"remainingFraction":0}]},
                {"displayName":"All off","buckets":[{"bucketId":"off","disabled":true}]},
                {"displayName":"Empty","buckets":[]}]}
            """);
        Assert.Equal("on", Assert.Single(snapshot.Groups[0].Windows).Id);
        Assert.Null(snapshot.Groups[0].Allowed);
        Assert.Empty(snapshot.Groups[1].Windows);
        Assert.False(snapshot.Groups[1].Allowed);
        Assert.Null(snapshot.Groups[2].Allowed);
    }

    [Fact]
    public void RepeatedOpaqueIdentifiersStayDistinctWithoutRenamingTheProviderLabels()
    {
        var snapshot = Parse("""
            {"groups":[
                {"displayName":"Same","buckets":[{"bucketId":"weekly","remainingFraction":0.5},{"bucketId":"weekly","remainingFraction":0.25}]},
                {"displayName":"Same","buckets":[{"window":"weekly","remainingFraction":0.75}]}]}
            """);
        Assert.Equal(["Same", "Same#1"], snapshot.Groups.Select(group => group.Id));
        Assert.Equal("Same", snapshot.Groups[1].Name);
        Assert.Equal(["weekly", "weekly#1"], snapshot.Groups[0].Windows.Select(window => window.Id));
        Assert.Equal(25, snapshot.Groups[0].Windows[1].RemainingPercent);
    }

    [Fact]
    public void UngroupedBucketsBecomeOneDefaultGroupAndUnknownWindowTokensStayOpaque()
    {
        var snapshot = Parse("""{"description":"Usage","buckets":[{"bucketId":"b1","window":"fortnightly","remainingFraction":0.5}]}""");
        var group = Assert.Single(snapshot.Groups);
        Assert.Equal("default", group.Id);
        Assert.Equal("Usage", group.Name);
        Assert.Null(Assert.Single(group.Windows).Duration);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("""{"groups":[{"displayName":"x","buckets":[{"remainingFraction":"not-a-number","disabled":"maybe"}]}]}""")]
    [InlineData("""{"groups":[{"displayName":"x","buckets":["not-an-object"]}]}""")]
    [InlineData("""{"groups":["not-an-object"]}""")]
    public void MalformedSummariesAreRejectedRatherThanPartiallyInterpreted(string json)
    {
        Assert.Equal(ProviderFailureKind.InvalidResponse,
            Assert.Throws<ProviderException>(() => Parse(json)).Kind);
    }

    [Fact]
    public void AnEmptySummaryIsReportedAsEmptyInsteadOfFabricatingAGroup()
    {
        Assert.Empty(Parse("""{"groups":[]}""").Groups);
        Assert.Empty(Parse("""{"buckets":[]}""").Groups);
    }
}
