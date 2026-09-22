using AiUsage.Core.Persistence;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers;
using System.Security.Cryptography;
using System.Text.Json;

namespace AiUsage.Infrastructure.Persistence;

/// <summary>Exclusive layout-0 to layout-1 migration. Never reads or restores provider records.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class StateMaintenance : IStateMaintenance, IDisposable
{
    private static readonly byte[] Entropy = "AiUsage.Checkpoint.v1"u8.ToArray();
    private readonly string root;
    private readonly Action<string>? boundary;
    private readonly Func<string, bool>? validatePreferences;
    private readonly SemaphoreSlim gate = new(1, 1);
    private FileStream? lease;
    private bool disposed;
    private string LayoutPath => Path.Combine(root, "layout.v1.json");
    private string LegacyPath => Path.Combine(root, "appearance.v1.json");
    private string TargetPath => Path.Combine(root, "preferences", "appearance.v1.json");
    private string JournalPath => Path.Combine(root, "maintenance", "journal.v1.json");
    private string CheckpointPath => Path.Combine(root, "maintenance", "checkpoint.v1.bin");

    public StateMaintenance(string ownedDirectory, Func<string, bool>? validatePreferences = null) : this(ownedDirectory, (Action<string>?)null)
    { this.validatePreferences = validatePreferences; }
    internal StateMaintenance(string ownedDirectory, Action<string>? boundary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownedDirectory);
        root = Path.GetFullPath(ownedDirectory);
        this.boundary = boundary;
    }

    public MaintenanceReport Current { get; private set; } = new(MaintenanceCondition.Interrupted, null, null);
    public Task<MaintenanceReport> InitializeAsync(CancellationToken cancellationToken) => RunAsync(false, null, cancellationToken);
    public Task<MaintenanceReport> RetryAsync(CancellationToken cancellationToken) => RunAsync(true, null, cancellationToken);
    public Task<MaintenanceReport> RestoreAsync(string checkpointId, CancellationToken cancellationToken) => RunAsync(true, checkpointId, cancellationToken);

    private async Task<MaintenanceReport> RunAsync(bool retry, string? restoreId, CancellationToken token)
    {
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return Current = await Task.Run(() => Execute(retry, restoreId, token), token).ConfigureAwait(false);
        }
        finally { gate.Release(); }
    }

    private MaintenanceReport Execute(bool retry, string? restoreId, CancellationToken token)
    {
        int? layout = null;
        MaintenanceCheckpoint? summary = null;
        var restoring = restoreId is not null;
        try
        {
            lease ??= ProviderStatePaths.Acquire(root, "state.lock");
            layout = ReadLayout();
            if (layout > 1) return new(MaintenanceCondition.NewerSchema, layout, null);
            var journal = restoreId is null ? ReadJournal() : null;
            restoring |= journal?.Operation == "restore";
            if (journal is not null || restoreId is not null)
            {
                var checkpoint = ReadCheckpoint();
                summary = Summary(checkpoint);
                if (journal is not null && journal.CheckpointId != checkpoint.Id) throw new IOException("Checkpoint mismatch.");
                if (!retry) return new(restoring ? MaintenanceCondition.RestoreFailed : MaintenanceCondition.Interrupted, layout, summary);
                if (restoreId is not null && restoreId != checkpoint.Id) throw new IOException("Unknown checkpoint.");
                if (restoring)
                {
                    WriteJournal(checkpoint, "restore");
                    boundary?.Invoke("restore-journal");
                    PublishTarget(checkpoint, token);
                    boundary?.Invoke("restore-target");
                    CommitLayout();
                    boundary?.Invoke("restore-commit");
                    // Explicit restore replaces the complete nonsecret durable generation. Credentials are independent.
                    DeleteOwned(LegacyPath);
                    DeleteOwned(JournalPath);
                }
                else Migrate(checkpoint, token);
                return new(MaintenanceCondition.Ready, 1, summary);
            }
            if (layout == 1)
            {
                ValidatePreferences(Read(TargetPath, 256 * 1024) ?? throw new IOException("Committed preferences missing."));
                return new(MaintenanceCondition.Ready, 1, null);
            }
            Check(TargetPath);
            if (File.Exists(TargetPath)) throw new IOException("Uncommitted target without journal.");
            var preferences = Read(LegacyPath, 256 * 1024);
            ValidatePreferences(preferences);
            var backup = new StateCheckpoint(1, 0, Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow,
                preferences is not null, preferences ?? [], Hash(preferences));
            WriteCheckpoint(backup);
            backup = ReadCheckpoint(); // Validate persisted protected bytes before publishing any migration intent.
            summary = Summary(backup);
            boundary?.Invoke("checkpoint");
            token.ThrowIfCancellationRequested();
            WriteJournal(backup, "migrate");
            boundary?.Invoke("journal");
            Migrate(backup, token);
            return new(MaintenanceCondition.Ready, 1, summary);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or CryptographicException or JsonException or ProviderException)
        {
            if (lease is not null && layout is 0 or 1 && summary is null)
            {
                try { summary = Summary(ReadCheckpoint()); }
                catch (Exception backupError) when (backupError is IOException or UnauthorizedAccessException or CryptographicException or JsonException or ProviderException) { }
            }
            return new(restoring ? MaintenanceCondition.RestoreFailed : MaintenanceCondition.Interrupted, layout, summary);
        }
    }

    private void Migrate(StateCheckpoint checkpoint, CancellationToken token)
    {
        // A legacy writer is not allowed to have changed the source since the verified checkpoint.
        var legacy = Read(LegacyPath, 256 * 1024);
        if (legacy is not null && !legacy.AsSpan().SequenceEqual(checkpoint.Preferences)) throw new IOException("Source changed.");
        PublishTarget(checkpoint, token);
        boundary?.Invoke("target");
        CommitLayout();
        boundary?.Invoke("commit");
        DeleteOwned(LegacyPath);
        boundary?.Invoke("cleanup");
        DeleteOwned(JournalPath);
    }

    private void PublishTarget(StateCheckpoint checkpoint, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var expected = checkpoint.HasPreferences ? checkpoint.Preferences : "{}"u8.ToArray();
        Write(TargetPath, expected);
        var staged = Read(TargetPath, 256 * 1024);
        if (staged is null || Hash(staged) != Hash(expected))
            throw new IOException("Target verification failed.");
        ValidatePreferences(staged);
        token.ThrowIfCancellationRequested();
    }

    private int ReadLayout()
    {
        var bytes = Read(LayoutPath, 4096);
        if (bytes is null) return 0;
        var record = JsonSerializer.Deserialize(bytes, MaintenanceJson.Default.StateLayout);
        if (record is null || record.Version != 1 || record.Layout < 1) throw new IOException("Invalid layout.");
        return record.Layout;
    }

    private StateJournal? ReadJournal()
    {
        var bytes = Read(JournalPath, 4096);
        if (bytes is null) return null;
        var journal = JsonSerializer.Deserialize(bytes, MaintenanceJson.Default.StateJournal);
        if (journal is null || journal.Version != 1 || journal.Source != 0 || journal.Target != 1 ||
            journal.Operation is not ("migrate" or "restore") || !Guid.TryParseExact(journal.CheckpointId, "N", out _))
            throw new IOException("Invalid journal.");
        return journal;
    }

    private void WriteJournal(StateCheckpoint checkpoint, string operation) =>
        Write(JournalPath, JsonSerializer.SerializeToUtf8Bytes(new StateJournal(1, 0, 1, operation, checkpoint.Id), MaintenanceJson.Default.StateJournal));
    private void CommitLayout() => Write(LayoutPath, JsonSerializer.SerializeToUtf8Bytes(new StateLayout(1, 1), MaintenanceJson.Default.StateLayout));

    private void WriteCheckpoint(StateCheckpoint checkpoint)
    {
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(checkpoint, MaintenanceJson.Default.StateCheckpoint);
        try { Write(CheckpointPath, ProtectedData.Protect(plaintext, Entropy, DataProtectionScope.CurrentUser)); }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    private StateCheckpoint ReadCheckpoint()
    {
        var ciphertext = Read(CheckpointPath, 512 * 1024) ?? throw new IOException("Missing checkpoint.");
        var plaintext = ProtectedData.Unprotect(ciphertext, Entropy, DataProtectionScope.CurrentUser);
        try
        {
            var record = JsonSerializer.Deserialize(plaintext, MaintenanceJson.Default.StateCheckpoint);
            if (record is null || record.Version != 1 || record.Layout != 0 ||
                !Guid.TryParseExact(record.Id, "N", out _) || record.CreatedAt == default ||
                record.Preferences is null or { Length: > 256 * 1024 } ||
                (!record.HasPreferences && record.Preferences.Length != 0) || Hash(record.Preferences) != record.Hash)
                throw new IOException("Invalid checkpoint.");
            if (record.HasPreferences) ValidatePreferences(record.Preferences);
            return record;
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    private static MaintenanceCheckpoint Summary(StateCheckpoint checkpoint) => new(checkpoint.Id, checkpoint.CreatedAt, checkpoint.Layout);
    private static string Hash(byte[]? bytes) => Convert.ToHexString(SHA256.HashData(bytes ?? []));

    private void ValidatePreferences(byte[]? bytes)
    {
        if (bytes is null) return;
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 64 });
        var content = document.RootElement;
        if (content.ValueKind != JsonValueKind.Object ||
            (content.TryGetProperty("Version", out var version) && (version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var value) || value != 1)) ||
            (validatePreferences is not null && !validatePreferences(System.Text.Encoding.UTF8.GetString(bytes))))
            throw new IOException("Unsupported preferences.");
    }

    /// <summary>Fixed, sanitized local export; never includes paths, identities, exception text or file contents.</summary>
    public async Task ExportDiagnosticsAsync(CancellationToken token)
    {
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            var text = $"AI Usage recovery\nCondition: {Current.Condition}\nLayout: {Current.LayoutVersion}\nCheckpoint available: {Current.Checkpoint is not null}\n";
            await Task.Run(() => Write(Path.Combine(root, "recovery-diagnostics.txt"), System.Text.Encoding.UTF8.GetBytes(text)), token).ConfigureAwait(false);
        }
        catch (ProviderException) { throw new IOException("Recovery diagnostic path is unavailable."); }
        finally { gate.Release(); }
    }

    private static byte[]? Read(string path, int limit)
    {
        Check(path);
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length <= 0 || stream.Length > limit) throw new IOException("Invalid state length.");
            var bytes = new byte[(int)stream.Length];
            stream.ReadExactly(bytes);
            return bytes;
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    private static void Write(string path, byte[] bytes)
    {
        Check(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        Check(path);
        var staged = path + ".new";
        Check(staged);
        using (var stream = new FileStream(staged, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        {
            stream.Write(bytes);
            stream.Flush(flushToDisk: true);
        }
        Check(path);
        Check(staged);
        File.Move(staged, path, overwrite: true);
    }

    private static void DeleteOwned(string path)
    {
        Check(path);
        if (File.Exists(path)) File.Delete(path);
    }

    private static void Check(string path)
    {
        ProviderStatePaths.CheckDirectory(Path.GetDirectoryName(path)!);
        ProviderStatePaths.CheckFile(path);
    }

    public void Dispose()
    {
        gate.Wait();
        try
        {
            disposed = true;
            lease?.Dispose();
            lease = null;
        }
        finally { gate.Release(); }
    }
}
