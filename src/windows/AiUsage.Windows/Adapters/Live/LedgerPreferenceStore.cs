using System.Text.Json;
using System.Text.Json.Serialization;
using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;

namespace AiUsage.Adapters.Live;

/// <summary>Ledger-only metadata. Provider-keyed labels/order never become account metadata.</summary>
internal sealed class LedgerPreferenceStore(Func<CancellationToken, Task<string?>> read,
    Func<string, CancellationToken, Task> write) : IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool writable;
    private bool stopped;
    public State Current { get; private set; } = new();

    public async Task<bool> LoadAsync(string? legacyJson, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            writable = false;
            if (stopped) return false;
            var json = await read(token);
            var loaded = json is null ? ImportGlobals(legacyJson) : JsonSerializer.Deserialize(json, LedgerPreferenceJson.Default.State);
            if (!Valid(loaded)) return false;
            Current = loaded!;
            writable = true;
            return true;
        }
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException) { return false; }
        finally { gate.Release(); }
    }

    public async Task<CommandOutcome> ChangeAsync(Func<State, State> change, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            if (!writable || stopped) return CommandOutcome.Unavailable;
            var next = change(Current);
            if (!Valid(next)) return CommandOutcome.Rejected;
            await write(JsonSerializer.Serialize(next, LedgerPreferenceJson.Default.State), token);
            Current = next;
            return CommandOutcome.Done;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return CommandOutcome.Unavailable; }
        finally { gate.Release(); }
    }

    public void Dispose() => gate.Dispose();

    public async Task StopAsync()
    {
        await gate.WaitAsync();
        try { stopped = true; }
        finally { gate.Release(); }
    }

    internal static bool IsValidJson(string json)
    {
        try { return Valid(JsonSerializer.Deserialize(json, LedgerPreferenceJson.Default.State)); }
        catch (JsonException) { return false; }
    }

    private static bool Valid(State? state) => state is { Version: 1, Preferences: not null, Labels: not null, Order: not null } &&
        Enum.IsDefined(state.Preferences.Mode) && Enum.IsDefined(state.Preferences.Density) && Enum.IsDefined(state.Preferences.Updates) &&
        state.Labels.Count <= 256 && state.Labels.All(x => Guid.TryParseExact(x.Key, "N", out var id) && id != Guid.Empty &&
            x.Value is { Length: > 0 and <= 100 } && x.Value == x.Value.Trim()) &&
        state.Order.Length <= 4096 && state.Order.All(x => x is { Length: > 0 and <= 8192 }) && state.Order.Distinct().Count() == state.Order.Length &&
        state.Hidden is { Length: <= 4096 } && state.Hidden.All(x => x is { Length: > 0 and <= 8192 }) && state.Hidden.Distinct().Count() == state.Hidden.Length &&
        state.Units is { Count: <= 4096 } && state.Units.All(x => x.Key is { Length: > 0 and <= 8192 } && x.Value is not null && CreditDollars.ValidRate(x.Value.Rate)) &&
        state.Today is { Length: <= 4096 } && state.Today.All(x => x is { Card.Length: > 0 and <= 8192, Instance.Length: > 0 and <= 8192, DayStart: >= 0 }) &&
            state.Today.DistinctBy(x => (x.Card, x.Date)).Count() == state.Today.Length &&
        (state.PendingWorkDays is null ? state.WorkDaysEffectiveOn is null :
            state.WorkDaysEffectiveOn is not null && state.PendingWorkDays.Length is > 0 and <= 7 &&
            state.PendingWorkDays.All(Enum.IsDefined) && state.PendingWorkDays.Distinct().Count() == state.PendingWorkDays.Length);

    private static State ImportGlobals(string? json)
    {
        if (json is null) return new();
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("Version", out var version) || !version.TryGetInt32(out var v) || v != 1)
                return new();
            int? Number(string name) => root.TryGetProperty(name, out var value) && value.TryGetInt32(out var n) ? n : null;
            bool Flag(string name) => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;
            return new() { Preferences = new(Number("UsageDisplay") == 0 ? ValueMode.Left : ValueMode.Used,
                Number("Density") == 0 ? Density.Comfortable : Density.Compact, Flag("ShowDisconnected"), Flag("AlwaysOnTop")) };
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException) { return new(); }
    }

    internal sealed record State
    {
        public int Version { get; set; } = 1;
        public LedgerPreferences Preferences { get; set; } = LedgerPreferences.Default;
        public Dictionary<string, string> Labels { get; set; } = [];
        public string[] Order { get; set; } = [];
        public string[] Hidden { get; set; } = [];
        /// <summary>D-199: per-card unit choice of a credit pool.</summary>
        public Dictionary<string, UnitModel> Units { get; set; } = [];
        /// <summary>D-199: the owner's day starts, one per card and local date.</summary>
        public TodayEntry[] Today { get; set; } = [];
        public DateOnly? WorkToday { get; set; }
        // Written only by earlier versions that deferred work-day changes to the next midnight; applied once (D-198).
        public DayOfWeek[]? PendingWorkDays { get; set; }
        public DateOnly? WorkDaysEffectiveOn { get; set; }
        [JsonExtensionData] public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }
}

[JsonSerializable(typeof(LedgerPreferenceStore.State))]
internal sealed partial class LedgerPreferenceJson : JsonSerializerContext;
