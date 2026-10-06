using System.Text.Json;
using System.Diagnostics;
using System.Security.Cryptography;

namespace AiUsage.Adapters.Live.Audit;

/// <summary>Validate the explicit offline selection before diagnostics or product composition touches storage.</summary>
internal sealed record AuditReplay(AuditInput Input, string Root, string InputPath, string FixtureSha256)
{
    internal const string Marker = "AI Usage synthetic audit v1";
    internal const int OpenFailureExitCode = 90;
    public string RestartArguments => "--demo --audit-input=\"" + InputPath + "\"";
    public void RecordProcess()
    {
        using var process = Process.GetCurrentProcess();
        var receipt = new AuditProcessReceipt(Marker, process.Id, process.StartTime.ToUniversalTime(), FixtureSha256, Input.Now, Input.ZoneId);
        File.WriteAllText(Path.Combine(Root, "process-" + process.Id + ".json"), JsonSerializer.Serialize(receipt, AuditJson.Default.AuditProcessReceipt));
    }

    public static AuditReplay? Open(string[] arguments, string? selectedRoot, bool packaged)
    {
        // The shipped package never replays audit input; only unpackaged development builds do.
        if (packaged) return null;
        var selections = arguments.Where(a => a.StartsWith("--audit-input=", StringComparison.Ordinal)).ToArray();
        if (selections.Length == 0) return null;
        if (selections.Length != 1 || !arguments.Contains("--demo", StringComparer.Ordinal))
            throw new InvalidOperationException("Audit replay requires one input and --demo");
        if (string.IsNullOrWhiteSpace(selectedRoot) || !Path.IsPathFullyQualified(selectedRoot))
            throw new InvalidOperationException("Audit replay requires an absolute isolated storage directory");

        var root = Path.GetFullPath(selectedRoot);
        var temporary = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath())) + Path.DirectorySeparatorChar;
        if (!root.StartsWith(temporary, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Audit storage must be a temporary directory");
        for (var path = new DirectoryInfo(root); path is not null; path = path.Parent)
            if (path.Exists && path.Attributes.HasFlag(FileAttributes.ReparsePoint))
                throw new InvalidOperationException("Audit storage must not redirect through a reparse point");

        var inputPath = Path.GetFullPath(selections[0]["--audit-input=".Length..]);
        if (inputPath.IndexOfAny(['\"', '\r', '\n']) >= 0) throw new InvalidDataException("Invalid synthetic input path");
        var bytes = File.ReadAllBytes(inputPath);
        var input = JsonSerializer.Deserialize(bytes, AuditJson.Default.AuditInput)
            ?? throw new InvalidDataException("Synthetic audit input is required");
        if (input.SyntheticMarker != Marker) throw new InvalidDataException("Synthetic audit marker is required");
        if (input.UseProductMaintenance && (input.Accounts.Length != 0 || input.Observations.Length != 0 ||
            input.Configuration.Caps.Count != 0 || input.NextAccounts is not null || input.NextNow is not null || input.Recovery is not null))
            throw new InvalidDataException("Product maintenance replay requires an empty synthetic account fixture");
        var marker = Path.Combine(root, "synthetic-audit.marker");
        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any() &&
            (!File.Exists(marker) || File.ReadAllText(marker) != Marker))
            throw new InvalidOperationException("Audit storage must be empty or an existing audit root");
        Directory.CreateDirectory(root);
        File.WriteAllText(marker, Marker);
        return new(input, root, inputPath, Convert.ToHexString(SHA256.HashData(bytes)));
    }
}
