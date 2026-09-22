using AiUsage.Features.Connection;

namespace AiUsage.Features.Presentation;

internal sealed record ProviderTileStyle(string Glyph, string BackgroundToken, string? ForegroundToken, uint? BackgroundRgb, bool ShowBorder);

/// <summary>Windows-owned identity and styling, shared by composition, live adapters and presentation.</summary>
internal sealed class ProviderCatalog
{
    public static ProviderCatalog Default { get; } = new(
    [
        new("codex", "Codex", "›_", [ConnectionMethod.BrowserSignIn], CapabilityOrigin.Existing)
        { UseThemeFill = true, DemoMethods = [ConnectionMethod.BrowserSignIn, ConnectionMethod.ManualCode] },
        new("claude", "Claude", "✳", [ConnectionMethod.BrowserSignIn, ConnectionMethod.ManualCode], CapabilityOrigin.Existing)
        { BrandRgb = 0xD77655 },
        new("copilot", "GitHub Copilot", "⊙", [ConnectionMethod.BrowserSignIn], CapabilityOrigin.Existing)
        { CompactName = "Copilot", BrandRgb = 0x5B6CFF, DemoOrigin = CapabilityOrigin.Planned },
        new("antigravity", "Antigravity", "↑", [ConnectionMethod.BrowserSignIn], CapabilityOrigin.Existing)
        { BrandRgb = 0x1BA39C, DemoOrigin = CapabilityOrigin.Planned }
    ]);

    private readonly IReadOnlyDictionary<string, ProviderDescriptor> byId;

    public ProviderCatalog(IEnumerable<ProviderDescriptor> descriptors)
    {
        All = Array.AsReadOnly(descriptors.Select(d => d with
        {
            Methods = Array.AsReadOnly(d.Methods.ToArray()),
            DemoMethods = d.DemoMethods is { } methods ? Array.AsReadOnly(methods.ToArray()) : null
        }).ToArray());
        byId = All.ToDictionary(d => d.ProviderId, StringComparer.Ordinal);
        Demo = Array.AsReadOnly(All.Select(d => d with
        {
            Name = d.PresentationName,
            Methods = d.DemoMethods ?? d.Methods,
            Origin = d.DemoOrigin ?? d.Origin
        }).ToArray());
    }

    public IReadOnlyList<ProviderDescriptor> All { get; }
    public IReadOnlyList<ProviderDescriptor> Demo { get; }

    public ProviderTileStyle Tile(string providerId, bool hues, bool highContrast)
    {
        var provider = Get(providerId);
        if (!hues || highContrast)
            return new(provider.Glyph, "Card2Brush", "TextBrush", null, true);
        if (provider.UseThemeFill)
            return new(provider.Glyph, "FillBrush", "AppBgBrush", null, false);
        return provider.BrandRgb is { } rgb
            ? new(provider.Glyph, "Card2Brush", null, rgb, false)
            : new(provider.Glyph, "Card2Brush", "TextBrush", null, false);
    }

    /// <summary>Opaque, ordinal protocol IDs are never translated or normalized. Unknown IDs use a neutral monogram.</summary>
    public ProviderDescriptor Get(string providerId) => byId.TryGetValue(providerId, out var descriptor)
        ? descriptor
        : new(providerId, providerId, providerId.Length > 0 ? providerId[..1].ToUpperInvariant() : "?", [], CapabilityOrigin.Existing);
}
