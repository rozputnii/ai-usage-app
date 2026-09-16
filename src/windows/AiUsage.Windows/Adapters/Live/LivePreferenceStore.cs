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
            var loaded = json is null ? new State() : JsonSerializer.Deserialize<State>(json);
            if (loaded is null || loaded.Version != 1 || !Enum.IsDefined(loaded.Theme) || !Enum.IsDefined(loaded.Density) ||
                !Enum.IsDefined(loaded.UsageDisplay) || loaded.Order is null || loaded.Hidden is null || loaded.Labels is null || loaded.Expansion is null ||
                loaded.Order.Any(id => id is null) || loaded.Hidden.Any(id => id is null) ||
                loaded.Labels.Values.Any(label => label is null || label.Length > 100) || loaded.Expansion.Values.Any(value => !Enum.IsDefined(value)))
                return;
            state = loaded;
            writable = true;
            Apply();
        }
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException) { writable = false; }
        finally { gate.Release(); }
    }
    public Task<UiCommandResult> SetPreferenceAsync(PreferenceChange change, CancellationToken cancellationToken) => ChangeAsync(s => change switch
    {
        { Key: PreferenceKey.Theme, Value: ThemePreference value } when Enum.IsDefined(value) => s with { Theme = value },
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
                await write(JsonSerializer.Serialize(next), token);
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
        Theme = state.Theme, Density = state.Density, UsageDisplay = state.UsageDisplay,
        AlwaysOnTop = state.AlwaysOnTop, ShowDisconnected = state.ShowDisconnected, ShowHidden = state.ShowHidden,
        AccountOrder = state.Order, HiddenTargets = state.Hidden
    }, state.Labels, state.Expansion);

    internal sealed record State
    {
        public int Version { get; init; } = 1;
        public ThemePreference Theme { get; init; }
        public Density Density { get; init; }
        public UsageDisplay UsageDisplay { get; init; }
        public bool AlwaysOnTop { get; init; }
        public bool ShowDisconnected { get; init; }
        public bool ShowHidden { get; init; }
        public string[] Order { get; init; } = [];
        public string[] Hidden { get; init; } = [];
        public Dictionary<string, string> Labels { get; init; } = [];
        public Dictionary<string, ExpansionPreference> Expansion { get; init; } = [];
        [JsonExtensionData] public Dictionary<string, JsonElement>? ExtensionData { get; init; }
    }
}
