using AiUsage.Adapters.Live;
using AiUsage.Core.Usage;
using AiUsage.Features.Accounts;
using AiUsage.Features.CliImport;
using AiUsage.Features.Connection;
using AiUsage.Features.Demo;
using AiUsage.Features.Overview;
using AiUsage.Features.Presentation;
using AiUsage.Features.Settings.Appearance;
using AiUsage.Features.Settings.Monitoring;
using AiUsage.Features.SystemStatusPage;
using AiUsage.Features.Tray;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class ProviderCatalogTests
{
    [Fact]
    public async Task FifthDescriptorReachesCompositionConnectionMappingAndEveryPresentationSurface()
    {
        const string id = "Fifth/opaque:ID";
        var descriptor = new ProviderDescriptor(id, "Fifth Provider", "★",
            [ConnectionMethod.BrowserSignIn, ConnectionMethod.ManualCode], CapabilityOrigin.Existing)
        { CompactName = "Synthetic brand", BrandRgb = 0x123456 };
        var catalog = new ProviderCatalog([.. ProviderCatalog.Default.All, descriptor]);
        var resolved = new List<string>();
        using var source = new LiveUsageSource(catalog, key =>
        {
            resolved.Add(key);
            return new Session(key == id);
        });
        await source.InitializeAsync();
        Assert.Equal(catalog.All.Select(d => d.ProviderId), resolved);
        var account = Assert.Single(source.Current.Accounts);
        Assert.Equal(id, account.ProviderId);
        Assert.Equal("Fifth Provider", account.Label);
        Assert.Equal("Fifth Provider", LiveMapping.Map(id, ProviderSessionState.NotConnected, false, catalog).Label);
        Assert.Equal(0x123456u, catalog.Get(id).BrandRgb);
        Assert.Equal("★", catalog.Get(id).Glyph);
        Assert.Equal(new ProviderTileStyle("★", "Card2Brush", null, 0x123456, false), catalog.Tile(id, true, false));
        Assert.Equal(new ProviderTileStyle("★", "Card2Brush", "TextBrush", null, true), catalog.Tile(id, false, false));
        Assert.Equal(new ProviderTileStyle("★", "Card2Brush", "TextBrush", null, true), catalog.Tile(id, true, true));

        using var host = new TestHost();
        var context = new PresentationContext(source, host.Dispatcher, host.Clock, host.Text,
            host.Announcer, host.Navigation, host.Dialogs, host.Motion, catalog);
        var flow = new LiveConnectionFlow(source, _ => { });
        using var add = new AddAccountViewModel(context, flow, new CliImportViewModel(context, host.Cli));
        var option = Assert.Single(add.ProviderOptions, p => p.ProviderId == id);
        Assert.Equal("Fifth Provider", option.Name);
        Assert.Equal("★", option.Glyph);
        Assert.Equal(descriptor.Methods, option.Descriptor.Methods);
        var demo = new DemoConnectionFlow(host.State, catalog);
        Assert.Equal("Synthetic brand", Assert.Single(demo.Providers, p => p.ProviderId == id).Name);

        using var overview = new OverviewViewModel(context, host.Preferences, add);
        var section = Assert.Single(overview.Sections);
        Assert.Equal("Synthetic brand", section.Name);
        Assert.Equal("★", section.Glyph);
        Assert.Contains("Synthetic brand", Assert.Single(section.Rows).AccessibleName);
        using var accounts = new AccountsViewModel(context, host.History, host.Data, host.Preferences, add);
        accounts.Select(id);
        Assert.Contains("Synthetic brand", Assert.Single(accounts.Items).AccessibleName);
        Assert.Equal("★", accounts.Detail.Glyph);
        Assert.Contains("Synthetic brand", accounts.Detail.Meta);
        using var tray = new TrayViewModel(context, host.Lifetime, () => Task.CompletedTask, () => Task.CompletedTask);
        Assert.Equal("★", Assert.Single(tray.Rows).Glyph);
        Assert.Contains("Synthetic brand", Assert.Single(tray.Rows).AccessibleName);
        using var appearance = new AppearanceSettingsViewModel(context, host.Preferences, host.Theme);
        Assert.Equal("Synthetic brand", Assert.Single(appearance.OrderItems).ProviderName);
        using var status = new SystemStatusViewModel(context, host.Diagnostics, () => Task.CompletedTask);
        Assert.Equal("Synthetic brand", Assert.Single(status.ProviderStatuses).ProviderName);
        using var monitoring = new MonitoringSettingsViewModel(context, host.Preferences, host.NotificationPreview, new ToastViewModel(context));
        Assert.Contains(monitoring.SharedRules, rule => rule.Label == "Synthetic brand");
        Assert.Contains("Synthetic brand", Assert.Single(monitoring.SharedRules, rule => rule.Scope == RuleScope.WindowType).SubText);
        Assert.Contains("Synthetic brand", Assert.Single(monitoring.AccountRules).Header);
        var candidate = new CliCandidateViewModel(new("synthetic", id, "Synthetic", "synthetic path", true), catalog);
        Assert.Equal("★", candidate.Glyph);
        await source.StopAsync();
    }

    [Fact]
    public void CatalogPreservesExistingNamesMethodsBrandsAndDemoDifferences()
    {
        var catalog = ProviderCatalog.Default;
        Assert.Equal(["codex", "claude", "copilot", "antigravity"], catalog.All.Select(d => d.ProviderId));
        Assert.Equal(["Codex", "Claude", "GitHub Copilot", "Antigravity"], catalog.All.Select(d => d.Name));
        Assert.Equal(["Codex", "Claude", "Copilot", "Antigravity"], catalog.All.Select(d => d.PresentationName));
        Assert.Equal(["›_", "✳", "⊙", "↑"], catalog.All.Select(d => d.Glyph));
        Assert.Equal([null, 0xD77655u, 0x5B6CFFu, 0x1BA39Cu], catalog.All.Select(d => d.BrandRgb));
        Assert.True(catalog.Get("codex").UseThemeFill);
        Assert.Equal(new ProviderTileStyle("›_", "FillBrush", "AppBgBrush", null, false), catalog.Tile("codex", true, false));
        Assert.Equal(new ProviderTileStyle("U", "Card2Brush", "TextBrush", null, false), catalog.Tile("unknown", true, false));
        Assert.Equal(["claude"], catalog.All.Where(d => d.Methods.Contains(ConnectionMethod.ManualCode)).Select(d => d.ProviderId));
        using var host = new TestHost();
        var demo = new DemoConnectionFlow(host.State, catalog);
        Assert.Equal(["codex", "claude"], demo.Providers.Where(d => d.Methods.Contains(ConnectionMethod.ManualCode)).Select(d => d.ProviderId));
        Assert.Equal(["copilot", "antigravity"], demo.Providers.Where(d => d.Origin == CapabilityOrigin.Planned).Select(d => d.ProviderId));
    }

    [Fact]
    public void CatalogKeepsUnknownIdsOpaqueAndCopiesInputCollections()
    {
        var methods = new List<ConnectionMethod> { ConnectionMethod.BrowserSignIn };
        var descriptors = new List<ProviderDescriptor> { new("Mixed/Id", "Name", "!", methods, CapabilityOrigin.Existing) };
        var catalog = new ProviderCatalog(descriptors);
        methods.Clear();
        descriptors.Clear();
        Assert.Single(catalog.All);
        Assert.Single(catalog.Get("Mixed/Id").Methods);
        Assert.Equal("mixed/id", catalog.Get("mixed/id").Name);
        Assert.Equal("M", catalog.Get("mixed/id").Glyph);
        Assert.Null(catalog.Get("mixed/id").BrandRgb);
        Assert.False(catalog.Get("mixed/id").UseThemeFill);
        Assert.Equal("?", catalog.Get("").Glyph);
        Assert.Throws<ArgumentException>(() => new ProviderCatalog([catalog.All[0], catalog.All[0]]));
    }

    [Fact]
    public async Task ManualCodeIsRejectedByUnsupportedUnknownAndStoppedSessions()
    {
        using var source = new LiveUsageSource(ProviderCatalog.Default, _ => new Session(false));
        var flow = new LiveConnectionFlow(source, _ => { });
        Assert.False(flow.TrySubmitCode(new("claude", ConnectionMethod.ManualCode, null), "synthetic"));
        Assert.False(flow.TrySubmitCode(new("unknown", ConnectionMethod.ManualCode, null), "synthetic"));
        await source.StopAsync();
        Assert.False(flow.TrySubmitCode(new("claude", ConnectionMethod.ManualCode, null), "synthetic"));
    }

    private sealed class Session(bool connected) : IProviderSession
    {
        public bool HasStoredGrant => connected;
        public ProviderSessionState State => connected
            ? new(ProviderSessionStatus.QuotaAvailable, new(DateTimeOffset.UtcNow, null,
                [new("group", "Usage", null, null, true, false, [new("window", 20, 80, null, null)])], null, null, null, null))
            : ProviderSessionState.NotConnected;
        public Task<ProviderSessionState> ReadCachedStateAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> ResumeAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> RefreshAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> DisconnectAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
    }
}
