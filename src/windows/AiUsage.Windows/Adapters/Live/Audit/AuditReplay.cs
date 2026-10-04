using System.Text.Json;

namespace AiUsage.Adapters.Live.Audit;

/// <summary>Validate the explicit offline selection before diagnostics or product composition touches storage.</summary>
internal sealed record AuditReplay(AuditInput Input, string Root)
{
    internal const string Marker = "AI Usage synthetic audit v1";

    public static AuditReplay? Open(string[] arguments, string? selectedRoot)
    {
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

        var input = JsonSerializer.Deserialize(File.ReadAllText(selections[0]["--audit-input=".Length..]), AuditJson.Default.AuditInput)
            ?? throw new InvalidDataException("Synthetic audit input is required");
        if (input.SyntheticMarker != Marker) throw new InvalidDataException("Synthetic audit marker is required");
        var marker = Path.Combine(root, "synthetic-audit.marker");
        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any() &&
            (!File.Exists(marker) || File.ReadAllText(marker) != Marker))
            throw new InvalidOperationException("Audit storage must be empty or an existing audit root");
        Directory.CreateDirectory(root);
        File.WriteAllText(marker, Marker);
        return new(input, root);
    }
}
