using System.Text.Json;
using System.Text.Json.Serialization;
using AiUsage.Features.Presentation;
using AiUsage.Features.Settings;

namespace AiUsage.Adapters.Live;

internal sealed class LivePreferenceStore(LiveUsageSource source, Func<CancellationToken, Task<string?>> read,
    Func<string, CancellationToken, Task> write) : IPreferenceStore
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private State state = new();
    private bool writable;
    private bool stopped;
    public async Task StopAsync()
    {
        await gate.WaitAsync();
        try { stopped = true; }
        finally { gate.Release(); }
    }
    public async Task LoadAsync(CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            var json = await read(token);
            var loaded = json is null ? new State() : JsonSerializer.Deserialize(json, PreferenceStateJson.Default.State);
            if (!IsValid(loaded))
                return;
            state = loaded!;
            writable = true;
            Apply();
        }
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException) { writable = false; }
        finally { gate.Release(); }
    }

    internal static bool IsValidJson(string json)
    {
        try { return IsValid(JsonSerializer.Deserialize(json, PreferenceStateJson.Default.State)); }
        catch (JsonException) { return false; }
    }
    private static bool IsValid(State? loaded) => loaded is not null && loaded.Version == 1 &&
        Enum.IsDefined(loaded.Density) && Enum.IsDefined(loaded.UsageDisplay) &&
        loaded.Order is not null && loaded.Hidden is not null && loaded.Labels is not null && loaded.Expansion is not null &&
        loaded.Order.All(id => id is not null) && loaded.Hidden.All(id => id is not null) &&
        loaded.Labels.Values.All(label => label is not null && label.Length <= 100) && loaded.Expansion.Values.All(Enum.IsDefined);
    public Task<UiCommandResult> SetPreferenceAsync(PreferenceChange change, CancellationToken cancellationToken) => ChangeAsync(s => change switch
    {
        { Key: PreferenceKey.Density, Value: Density value } when Enum.IsDefined(value) => s with { Density = value },
        { Key: PreferenceKey.UsageDisplay, Value: UsageDisplay value } when Enum.IsDefined(value) => s with { UsageDisplay = value },
        { Key: PreferenceKey.AlwaysOnTop, Value: bool value } => s with { AlwaysOnTop = value },
        { Key: PreferenceKey.ShowDisconnected, Value: bool value } => s with { ShowDisconnected = value },
        { Key: PreferenceKey.ShowHidden, Value: bool value } => s with { ShowHidden = value },
        _ => null
    }, cancellationToken);
    public Task<UiCommandResult> SetNotificationRuleAsync(NotificationRule rule, CancellationToken cancellationToken) => Task.FromResult(UiCommandResult.Unsupported);
    public Task<UiCommandResult> ResetSettingsAsync(CancellationToken cancellationToken) => ChangeAsync(s => new State
    { Labels = s.Labels, Order = s.Order, Hidden = s.Hidden, Expansion = s.Expansion, ExtensionData = s.ExtensionData }, cancellationToken);

    internal Task<UiCommandResult> ExecuteAsync(UiCommand command, CancellationToken token) => ChangeAsync(s =>
    {
        if (command.Kind == UiCommandKind.Reorder && command.Payload is ReorderPayload order &&
            order.Order.Distinct().Count() == order.Order.Count && order.Order.All(id => source.Current.Accounts.Any(a => a.Id == id)))
            return s with { Order = order.Order.ToArray() };
        var id = command.TargetId;
        var account = source.Current.Accounts.FirstOrDefault(a => a.Id == id);
        var validTarget = source.Current.Accounts.Any(a => a.Id == id || a.Contexts.Any(c => c.Id == id || c.Groups.Any(g => g.Id == id)));
        if (!validTarget || id is null) return null;
        if (command.Kind == UiCommandKind.Rename && account is not null && command.Payload is RenamePayload rename && rename.Label.Trim().Length is > 0 and <= 100)
            return s with { Labels = new(s.Labels) { [id] = rename.Label.Trim() } };
        if (command.Kind == UiCommandKind.SetVisibility && command.Payload is VisibilityPayload { MuteAlerts: false } visibility)
            return s with { Hidden = visibility.Hidden ? s.Hidden.Append(id).Distinct().ToArray() : s.Hidden.Where(value => value != id).ToArray() };
        if (command.Kind == UiCommandKind.SetExpansion && command.Payload is ExpansionPayload expansion && Enum.IsDefined(expansion.Expansion))
            return s with { Expansion = new(s.Expansion) { [id] = expansion.Expansion } };
        return null;
    }, token);

    private async Task<UiCommandResult> ChangeAsync(Func<State, State?> change, CancellationToken token)
    {
        try
        {
            await gate.WaitAsync(token);
            try
            {
                if (stopped) return UiCommandResult.Cancelled;
                var next = change(state);
                if (next is null) return UiCommandResult.Unsupported;
                if (!writable) return UiCommandResult.Failed();
                await write(JsonSerializer.Serialize(next, PreferenceStateJson.Default.State), token);
                state = next;
                Apply();
                return UiCommandResult.Succeeded;
            }
            finally { gate.Release(); }
        }
        catch (OperationCanceledException) { return UiCommandResult.Cancelled; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return UiCommandResult.Failed(); }
    }
    private void Apply() => source.SetPreferences(source.Current.Preferences with
    {
        Density = state.Density, UsageDisplay = state.UsageDisplay,
        AlwaysOnTop = state.AlwaysOnTop, ShowDisconnected = state.ShowDisconnected, ShowHidden = state.ShowHidden,
        AccountOrder = state.Order, HiddenTargets = state.Hidden
    }, state.Labels, state.Expansion);

    /// <summary>
    /// The persisted preference file. The members are settable rather than init-only because the
    /// source generator turns init-only members into constructor parameters: extension data cannot
    /// bind to one, and a member absent from the file would arrive as null instead of the default
    /// below. Instances are still replaced wholesale through <c>with</c>, never mutated in place.
    /// </summary>
    internal sealed record State
    {
        public int Version { get; set; } = 1;
        public Density Density { get; set; }
        public UsageDisplay UsageDisplay { get; set; }
        public bool AlwaysOnTop { get; set; }
        public bool ShowDisconnected { get; set; }
        public bool ShowHidden { get; set; }
        public string[] Order { get; set; } = [];
        public string[] Hidden { get; set; } = [];
        public Dictionary<string, string> Labels { get; set; } = [];
        public Dictionary<string, ExpansionPreference> Expansion { get; set; } = [];
        [JsonExtensionData] public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }
}

// Preferences keep [JsonExtensionData] and deliberately do not set UnmappedMemberHandling.Disallow,
// unlike the credential stores: a file written by a newer build must survive a round-trip here.
[JsonSerializable(typeof(LivePreferenceStore.State))]
internal sealed partial class PreferenceStateJson : JsonSerializerContext;
