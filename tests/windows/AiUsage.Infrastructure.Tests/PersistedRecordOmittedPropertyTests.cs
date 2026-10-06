using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
/// JSON that omits one property. An omittable property must keep its non-default initializer. A required
/// member (registry and stored-state version, registry lists) reads as the type default, and the owning
/// read path must reject that record. Each probe also writes how the metadata binds the property, as report evidence.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class PersistedRecordOmittedPropertyTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "AiUsage.OmittedProperty.Tests", Guid.NewGuid().ToString("N"));
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private const string AccountJson = """
        {"Id":"00000000-0000-4000-8000-000000000011","Provider":"claude","StorageId":"00000000-0000-4000-8000-000000000012",
         "Identity":{"Subject":"synthetic-subject","Context":null},"Disconnecting":false,"LegacyStorage":false}
        """;

    private const string RegistryJson = """{"Version":1,"Revision":"00000000-0000-4000-8000-000000000001","ParentRevision":null,"Accounts":[],"Pending":[],"LegacyMigrationComplete":false}""";
    private const string AntigravityJson = """{"Version":2,"Revision":"00000000-0000-4000-8000-000000000001","ParentRevision":null,"AccountId":"synthetic-account","RefreshToken":"synthetic-refresh","ProjectId":"synthetic-project","Tier":null,"NeedsReauthentication":false,"CachedQuota":null}""";
    private const string ClaudeJson = """{"Version":2,"Revision":"00000000-0000-4000-8000-000000000001","ParentRevision":null,"Identity":{"AccountId":"synthetic-account","OrganizationId":"synthetic-organization"},"RefreshToken":"synthetic-refresh","NeedsReauthentication":false,"CachedQuota":null}""";
    private const string CopilotJson = """{"Version":2,"Revision":"00000000-0000-4000-8000-000000000001","ParentRevision":null,"AccountId":"123","AccessToken":"synthetic-access","ExpiresAt":null,"NeedsReauthentication":false,"CachedQuota":null}""";

    [Fact]
    public void AccountRecordOmittingConnectedKeepsTrue()
    {
        var state = Read($$"""{"Version":1,"Revision":"00000000-0000-4000-8000-000000000001","ParentRevision":null,"Accounts":[{{AccountJson}}],"Pending":[],"LegacyMigrationComplete":false}""",
            AccountRegistryJson.Default.AccountRegistryState);
        RecordBinding(AccountRegistryJson.Default.AccountRecord, nameof(AccountRecord.Connected));
        Assert.True(Assert.Single(state.Accounts).Connected);
    }

    // AccountRegistryState has no parameterized constructor: it answers ANL-11's open question. A registry
    // without Version, Accounts or Pending is malformed; loading it as an empty current registry could let a
    // later save overwrite recoverable data, so the lease must fail closed and keep the bytes.
    [Fact]
    public async Task AccountRegistryStateOmittingVersionIsRejectedByTheLease()
    {
        var json = Without(RegistryJson, "Version");
        var state = Read(json, AccountRegistryJson.Default.AccountRegistryState);
        RecordBinding(AccountRegistryJson.Default.AccountRegistryState, nameof(AccountRegistryState.Version));
        Assert.Equal(0, state.Version);
        await AssertOnlyCompleteRecordLoads("accounts.state", "AiUsage.AccountRegistry.v1", RegistryJson, json,
            () => new AccountRegistry(root).ReadAsync(Token));
    }

    [Fact]
    public async Task AccountRegistryStateOmittingAccountsIsRejectedByTheLease()
    {
        var json = Without(RegistryJson, "Accounts");
        var state = Read(json, AccountRegistryJson.Default.AccountRegistryState);
        RecordBinding(AccountRegistryJson.Default.AccountRegistryState, nameof(AccountRegistryState.Accounts));
        Assert.Null(state.Accounts);
        await AssertOnlyCompleteRecordLoads("accounts.state", "AiUsage.AccountRegistry.v1", RegistryJson, json,
            () => new AccountRegistry(root).ReadAsync(Token));
    }

    [Fact]
    public async Task AccountRegistryStateOmittingPendingIsRejectedByTheLease()
    {
        var json = Without(RegistryJson, "Pending");
        var state = Read(json, AccountRegistryJson.Default.AccountRegistryState);
        RecordBinding(AccountRegistryJson.Default.AccountRegistryState, nameof(AccountRegistryState.Pending));
        Assert.Null(state.Pending);
        await AssertOnlyCompleteRecordLoads("accounts.state", "AiUsage.AccountRegistry.v1", RegistryJson, json,
            () => new AccountRegistry(root).ReadAsync(Token));
    }

    // A stored grant without Version is malformed: ProviderStateMigration.Decode rejects it before deserializing.
    [Fact]
    public async Task AntigravityStoredStateOmittingVersionIsRejectedByTheMigration()
    {
        var json = Without(AntigravityJson, "Version");
        var state = Read(json, AntigravityStateJson.Default.AntigravityStoredState);
        RecordBinding(AntigravityStateJson.Default.AntigravityStoredState, nameof(AntigravityStoredState.Version));
        Assert.Equal(0, state.Version);
        await AssertOnlyCompleteRecordLoads("antigravity.state", "AiUsage.Antigravity.State.v1", AntigravityJson, json, async () =>
        {
            await using var lease = await new AntigravityStateStore(root).AcquireAsync(Token);
            return await lease.LoadAsync(Token);
        });
    }

    [Fact]
    public async Task ClaudeStoredStateOmittingVersionIsRejectedByTheMigration()
    {
        var json = Without(ClaudeJson, "Version");
        var state = Read(json, ClaudeStateJson.Default.ClaudeStoredState);
        RecordBinding(ClaudeStateJson.Default.ClaudeStoredState, nameof(ClaudeStoredState.Version));
        Assert.Equal(0, state.Version);
        await AssertOnlyCompleteRecordLoads("claude.state", "AiUsage.Claude.State.v1", ClaudeJson, json, async () =>
        {
            await using var lease = await new ClaudeStateStore(root).AcquireAsync(Token);
            return await lease.LoadAsync(Token);
        });
    }

    [Fact]
    public async Task CopilotStoredStateOmittingVersionIsRejectedByTheMigration()
    {
        var json = Without(CopilotJson, "Version");
        var state = Read(json, CopilotStateJson.Default.CopilotStoredState);
        RecordBinding(CopilotStateJson.Default.CopilotStoredState, nameof(CopilotStoredState.Version));
        Assert.Equal(0, state.Version);
        await AssertOnlyCompleteRecordLoads("copilot.state", "AiUsage.Copilot.State.v1", CopilotJson, json, async () =>
        {
            await using var lease = await new CopilotStateStore(root).AcquireAsync(Token);
            return await lease.LoadAsync(Token);
        });
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

    private static string Without(string json, string member)
    {
        var node = JsonNode.Parse(json)!.AsObject();
        Assert.True(node.Remove(member));
        return node.ToJsonString();
    }

    // The complete record loads through the production read path, so the rejection is caused by the omission alone;
    // the rejected protected bytes stay in place for recovery.
    private async Task AssertOnlyCompleteRecordLoads<T>(string file, string entropy, string complete, string omitted, Func<Task<T>> load)
    {
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, file);
        await File.WriteAllBytesAsync(path, Protect(complete, entropy), Token);
        Assert.NotNull(await load());
        var bytes = Protect(omitted, entropy);
        await File.WriteAllBytesAsync(path, bytes, Token);
        var error = await Assert.ThrowsAsync<ProviderException>(async () => await load());
        Assert.Equal(ProviderFailureKind.RecoveryRequired, error.Kind);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(path, Token));
    }

    private static byte[] Protect(string json, string entropy) =>
        ProtectedData.Protect(Encoding.UTF8.GetBytes(json), Encoding.UTF8.GetBytes(entropy), DataProtectionScope.CurrentUser);

    private static void RecordBinding<T>(JsonTypeInfo<T> type, string property)
    {
        var parameter = type.Properties.Single(p => p.Name == property).AssociatedParameter;
        var binding = parameter is null ? "property setter, assigned only when present"
            : parameter.IsMemberInitializer ? $"member initializer at construction, hasDefault={parameter.HasDefaultValue}"
            : $"constructor parameter, hasDefault={parameter.HasDefaultValue}, default={parameter.DefaultValue ?? "null"}";
        TestContext.Current.TestOutputHelper?.WriteLine($"{typeof(T).Name}.{property}: {binding}");
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
