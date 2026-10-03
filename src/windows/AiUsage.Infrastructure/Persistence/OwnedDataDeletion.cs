using System.Text;
using AiUsage.Infrastructure.Diagnostics;
using AiUsage.Infrastructure.Providers;

namespace AiUsage.Infrastructure.Persistence;

/// <summary>
/// Confirmed local reset, after all product and diagnostic writers have drained. Exact owned names
/// only; a durable intent survives partial deletion. Holds the root lease until completion.
/// </summary>
public sealed class OwnedDataDeletion
{
    private const string IntentName = "delete-local-data.v1.json";
    private const string Intent = "{\"version\":1,\"operation\":\"delete-local-data\"}";
    private readonly string root;
    private readonly Action<string>? boundary;
    private static readonly string[] ProviderNames = ["claude.state", "codex.grant", "codex.quota.json", "copilot.state", "antigravity.state"];
    private static readonly string[] ProviderFiles = ProviderNames.SelectMany(name => name switch
    {
        "codex.grant" => new[] { name, name + ".pending", name + ".new", name + ".lock" },
        "codex.quota.json" => new[] { name, name + ".new", name + ".v1.bak", name + ".v1.bak.new", name + ".lock" },
        _ => new[] { name, name + ".pending", name + ".v1.bak", name + ".v1.bak.new", name + ".v2.new", name + ".lock" }
    }).ToArray();

    public OwnedDataDeletion(string ownedRoot) : this(ownedRoot, null) { }
    internal OwnedDataDeletion(string ownedRoot, Action<string>? boundary)
    { root = Path.GetFullPath(ownedRoot); this.boundary = boundary; }

    public bool Pending
    {
        get
        {
            Check(Path.Combine(root, IntentName)); Check(Path.Combine(root, IntentName + ".new"));
            return File.Exists(Path.Combine(root, IntentName)) || File.Exists(Path.Combine(root, IntentName + ".new"));
        }
    }

    public Task RunAsync(bool confirmed, CancellationToken token) => Task.Run(async () =>
    {
        token.ThrowIfCancellationRequested();
        using var rootLease = ProviderStatePaths.Acquire(root, "state.lock");
        var intentPath = Path.Combine(root, IntentName);
        if (Pending)
        {
            var authoritative = File.Exists(intentPath) ? intentPath : intentPath + ".new";
            if (new FileInfo(authoritative).Length != Encoding.UTF8.GetByteCount(Intent) || File.ReadAllText(authoritative) != Intent)
                throw new IOException("The deletion intent is unsupported; data is preserved.");
            if (authoritative != intentPath) File.Move(authoritative, intentPath);
        }
        else if (!confirmed) throw new InvalidOperationException("Stored-data deletion requires confirmation.");
        // Once the confirmation is durable, cancellation cannot turn a partial wipe into an ordinary startup.
        if (!File.Exists(intentPath)) Write(intentPath, Intent);
        boundary?.Invoke("intent");
        using var registryLease = ProviderStatePaths.Acquire(root, "accounts.state.lock");
        var providerDirectories = ProviderDirectories();
        var metadata = MetadataFiles();
        var leases = new List<FileStream>();
        try
        {
            boundary?.Invoke("before-provider-leases");
            // Also exclude older standalone provider clients that do not take the product root lease.
            foreach (var directory in providerDirectories)
                foreach (var name in ProviderNames) leases.Add(ProviderStatePaths.Acquire(directory, name + ".lock"));
            var providerPaths = providerDirectories.SelectMany(d => OwnedFiles(d, ProviderFiles)).ToArray();
            foreach (var path in providerPaths.Where(p => !p.EndsWith(".lock", StringComparison.Ordinal))) Delete(path);
            boundary?.Invoke("provider-data");
            using var budget = new LocalBudgetStore(root);
            await budget.DeleteAllAsync(CancellationToken.None).ConfigureAwait(false);
            boundary?.Invoke("budget-data");
            foreach (var path in metadata) Delete(path);
            DiagnosticFiles.DeleteOwned(root);
            boundary?.Invoke("metadata");
            // Keep the forward fence: an old binary must never resurrect legacy state after reset.
            Write(Path.Combine(root, "preferences", "appearance.v1.json"), "{}");
            Write(Path.Combine(root, "layout.v1.json"), "{\"Version\":1,\"Layout\":2}");
            Delete(intentPath + ".new");
            Delete(intentPath); // Last operation: no predecessor/checkpoint can restore deleted data.
        }
        finally { foreach (var lease in leases) lease.Dispose(); }
    }, token);

    private string[] ProviderDirectories()
    {
        var result = new List<string>();
        var legacy = Path.Combine(root, "providers");
        ProviderStatePaths.CheckDirectory(legacy);
        result.Add(legacy);
        var accounts = Path.Combine(root, "accounts");
        ProviderStatePaths.CheckDirectory(accounts);
        if (Directory.Exists(accounts))
            foreach (var entry in Directory.EnumerateFileSystemEntries(accounts))
            {
                ProviderStatePaths.CheckDirectory(entry);
                if (!Guid.TryParseExact(Path.GetFileName(entry), "N", out var id) || id == Guid.Empty)
                    throw new IOException("Unknown account data is preserved.");
                result.Add(entry);
            }
        return result.ToArray();
    }

    private string[] MetadataFiles()
    {
        var files = new List<string>();
        foreach (var name in new[] { "accounts.state", "accounts.state.pending", "appearance.v1.json", "appearance.v1.json.new",
            "recovery-diagnostics.txt", "recovery-diagnostics.txt.new", "layout.v1.json.new" })
        { var path = Path.Combine(root, name); Check(path); if (File.Exists(path)) files.Add(path); }
        files.AddRange(OwnedFiles(Path.Combine(root, "maintenance"),
            ["checkpoint.v1.bin", "checkpoint.v1.bin.new", "journal.v1.json", "journal.v1.json.new"]));
        files.AddRange(PreferenceFiles(Path.Combine(root, "preferences"), allowLedger: true));
        return files.ToArray();
    }

    private static IEnumerable<string> PreferenceFiles(string directory, bool allowLedger)
    {
        ProviderStatePaths.CheckDirectory(directory);
        if (!Directory.Exists(directory)) return [];
        var files = new List<string>();
        foreach (var path in Directory.EnumerateFileSystemEntries(directory))
        {
            var name = Path.GetFileName(path);
            if (allowLedger && name == "ledger") { files.AddRange(PreferenceFiles(path, false)); continue; }
            Check(path);
            bool temporary = name.StartsWith("appearance-", StringComparison.Ordinal) && name.EndsWith(".tmp", StringComparison.Ordinal) &&
                Guid.TryParseExact(name[11..^4], "N", out _);
            if (name is not ("appearance.v1.json" or "appearance.v1.json.new") && !temporary)
                throw new IOException("Unknown preference data is preserved.");
            files.Add(path);
        }
        return files;
    }

    private static IEnumerable<string> OwnedFiles(string directory, IReadOnlyCollection<string> names)
    {
        ProviderStatePaths.CheckDirectory(directory);
        if (!Directory.Exists(directory)) return [];
        var files = Directory.GetFileSystemEntries(directory);
        foreach (var path in files)
        {
            Check(path);
            if (!names.Contains(Path.GetFileName(path), StringComparer.Ordinal)) throw new IOException("Unknown owned-namespace data is preserved.");
        }
        return files;
    }
    private static void Check(string path)
    { ProviderStatePaths.CheckDirectory(Path.GetDirectoryName(path)!); ProviderStatePaths.CheckFile(path); }
    private static void Delete(string path) { Check(path); File.Delete(path); }
    private static void Write(string path, string content)
    {
        Check(path); Check(path + ".new");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using (var file = new FileStream(path + ".new", FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        { file.Write(Encoding.UTF8.GetBytes(content)); file.Flush(true); }
        Check(path); Check(path + ".new"); File.Move(path + ".new", path, true);
    }
}
