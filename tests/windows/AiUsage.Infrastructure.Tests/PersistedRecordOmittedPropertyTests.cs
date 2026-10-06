using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AiUsage.Adapters.Live.Audit;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Accounts;
using AiUsage.Infrastructure.Providers;
using AiUsage.Infrastructure.Providers.Antigravity;
using AiUsage.Infrastructure.Providers.Claude;
using AiUsage.Infrastructure.Providers.Copilot;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

/// <summary>
/// ANL-11 probes: each persisted record is read through its production source-generated context from
/// JSON that omits one property, and the probe asserts that the property's non-default initializer
/// survives. Each probe also writes how the metadata binds that property, as report evidence.
/// </summary>
public sealed class PersistedRecordOmittedPropertyTests
{
    private const string AccountJson = """
        {"Id":"00000000-0000-4000-8000-000000000011","Provider":"claude","StorageId":"00000000-0000-4000-8000-000000000012",
         "Identity":{"Subject":"synthetic-subject","Context":null},"Disconnecting":false,"LegacyStorage":false}
        """;

    [Fact]
    public void AccountRecordOmittingConnectedKeepsTrue()
    {
        var state = Read($$"""{"Version":1,"Revision":"00000000-0000-4000-8000-000000000001","ParentRevision":null,"Accounts":[{{AccountJson}}],"Pending":[],"LegacyMigrationComplete":false}""",
            AccountRegistryJson.Default.AccountRegistryState);
        RecordBinding(AccountRegistryJson.Default.AccountRecord, nameof(AccountRecord.Connected));
        Assert.True(Assert.Single(state.Accounts).Connected);
    }

    // AccountRegistryState has no parameterized constructor: it answers ANL-11's open question.
    [Fact]
    public void AccountRegistryStateOmittingVersionKeepsOne()
    {
        var state = Read("""{"Revision":"00000000-0000-4000-8000-000000000001","ParentRevision":null,"Accounts":[],"Pending":[],"LegacyMigrationComplete":false}""",
            AccountRegistryJson.Default.AccountRegistryState);
        RecordBinding(AccountRegistryJson.Default.AccountRegistryState, nameof(AccountRegistryState.Version));
        Assert.Equal(1, state.Version);
    }

    [Fact]
    public void AccountRegistryStateOmittingAccountsKeepsEmptyList()
    {
        var state = Read("""{"Version":1,"Revision":"00000000-0000-4000-8000-000000000001","ParentRevision":null,"Pending":[],"LegacyMigrationComplete":false}""",
            AccountRegistryJson.Default.AccountRegistryState);
        RecordBinding(AccountRegistryJson.Default.AccountRegistryState, nameof(AccountRegistryState.Accounts));
        Assert.NotNull(state.Accounts);
        Assert.Empty(state.Accounts);
    }

    [Fact]
    public void AccountRegistryStateOmittingPendingKeepsEmptyList()
    {
        var state = Read("""{"Version":1,"Revision":"00000000-0000-4000-8000-000000000001","ParentRevision":null,"Accounts":[],"LegacyMigrationComplete":false}""",
            AccountRegistryJson.Default.AccountRegistryState);
        RecordBinding(AccountRegistryJson.Default.AccountRegistryState, nameof(AccountRegistryState.Pending));
        Assert.NotNull(state.Pending);
        Assert.Empty(state.Pending);
    }

    [Fact]
    public void AntigravityStoredStateOmittingVersionKeepsTwo()
    {
        var state = Read("""{"Revision":"00000000-0000-4000-8000-000000000001","ParentRevision":null,"AccountId":"synthetic-account","RefreshToken":"synthetic-refresh","ProjectId":"synthetic-project","Tier":null,"NeedsReauthentication":false,"CachedQuota":null}""",
            AntigravityStateJson.Default.AntigravityStoredState);
        RecordBinding(AntigravityStateJson.Default.AntigravityStoredState, nameof(AntigravityStoredState.Version));
        Assert.Equal(2, state.Version);
    }

    [Fact]
    public void ClaudeStoredStateOmittingVersionKeepsTwo()
    {
        var state = Read("""{"Revision":"00000000-0000-4000-8000-000000000001","ParentRevision":null,"Identity":{"AccountId":"synthetic-account","OrganizationId":"synthetic-organization"},"RefreshToken":"synthetic-refresh","NeedsReauthentication":false,"CachedQuota":null}""",
            ClaudeStateJson.Default.ClaudeStoredState);
        RecordBinding(ClaudeStateJson.Default.ClaudeStoredState, nameof(ClaudeStoredState.Version));
        Assert.Equal(2, state.Version);
    }

    [Fact]
    public void CopilotStoredStateOmittingVersionKeepsTwo()
    {
        var state = Read("""{"Revision":"00000000-0000-4000-8000-000000000001","ParentRevision":null,"AccountId":"synthetic-account","AccessToken":"synthetic-access","ExpiresAt":null,"NeedsReauthentication":false,"CachedQuota":null}""",
            CopilotStateJson.Default.CopilotStoredState);
        RecordBinding(CopilotStateJson.Default.CopilotStoredState, nameof(CopilotStoredState.Version));
        Assert.Equal(2, state.Version);
    }

    // ReadingObservation reaches disk through a source-generated context only in audit replay input
    // (AuditJson); LocalBudgetStore persists StoredRun instead, which has no rounding unit.
    [Fact]
    public void ReadingObservationOmittingRoundingUnitKeepsOne()
    {
        var observation = Read("""
            {"Series":{"AccountTarget":"synthetic-account","Limit":{"Provider":"codex","Family":"session","NativeDiscriminator":"primary"}},
             "Value":{"kind":"count","value":40,"unit":"percent"},"FetchedAt":"2026-10-06T00:00:00+00:00","PlanType":null,"Source":0,
             "SourceVersion":null,"UsedPercent":40,"ResetAt":null,"ResetPrecision":null,"PeriodStartedAt":null,"IsBalance":false}
            """, AuditJson.Default.ReadingObservation);
        RecordBinding(AuditJson.Default.ReadingObservation, nameof(ReadingObservation.RoundingUnit));
        Assert.Equal(1m, observation.RoundingUnit);
    }

    // Contrast probe: a positional default (constructor parameter), persisted inside cached quota.
    [Fact]
    public void OpaqueQuotaAmountOmittingUnitKeepsUnknown()
    {
        var amount = Read("""{"Used":"1","Limit":"10","Remaining":"9"}""", CopilotStateJson.Default.OpaqueQuotaAmount);
        RecordBinding(CopilotStateJson.Default.OpaqueQuotaAmount, nameof(OpaqueQuotaAmount.Unit));
        Assert.Equal("unknown", amount.Unit);
    }

    private static T Read<T>(string json, JsonTypeInfo<T> type) =>
        JsonSerializer.Deserialize(json, type) ?? throw new InvalidDataException("Probe JSON produced null.");

    private static void RecordBinding<T>(JsonTypeInfo<T> type, string property)
    {
        var parameter = type.Properties.Single(p => p.Name == property).AssociatedParameter;
        var binding = parameter is null ? "property setter, assigned only when present"
            : parameter.IsMemberInitializer ? $"member initializer at construction, hasDefault={parameter.HasDefaultValue}"
            : $"constructor parameter, hasDefault={parameter.HasDefaultValue}, default={parameter.DefaultValue ?? "null"}";
        TestContext.Current.TestOutputHelper?.WriteLine($"{typeof(T).Name}.{property}: {binding}");
    }
}
