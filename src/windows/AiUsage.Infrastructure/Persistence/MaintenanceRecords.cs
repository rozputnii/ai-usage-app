using System.Text.Json.Serialization;

namespace AiUsage.Infrastructure.Persistence;

internal sealed record StateLayout(int Version, int Layout);
internal sealed record StateJournal(int Version, int Source, int Target, string Operation, string CheckpointId);
internal sealed record StateCheckpoint(int Version, int Layout, string Id, DateTimeOffset CreatedAt, bool HasPreferences, byte[] Preferences, string Hash);

[JsonSourceGenerationOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 64)]
[JsonSerializable(typeof(StateLayout))]
[JsonSerializable(typeof(StateJournal))]
[JsonSerializable(typeof(StateCheckpoint))]
internal partial class MaintenanceJson : JsonSerializerContext;
