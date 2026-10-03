using System.Globalization;
using System.Text.RegularExpressions;

namespace AiUsage.Infrastructure.Diagnostics;

/// <summary>Exact owned names only. Never follows a redirected ancestor or recursively deletes.</summary>
public static partial class DiagnosticFiles
{
    internal const string TimestampFormat = "yyyyMMddTHHmmssfffffffZ";

    [GeneratedRegex(@"^(application|trace|response|critical|session)-([0-9]{8}T[0-9]{13}Z)-[a-f0-9]{32}(?:_[0-9]+)?\.(jsonl|json|stage|open)$", RegexOptions.CultureInvariant)]
    private static partial Regex OwnedName();

    private static bool IsOwned(string name)
    {
        var match = OwnedName().Match(name);
        if (!match.Success) return false;
        return (match.Groups[1].Value, match.Groups[3].Value) switch
        {
            ("application" or "trace" or "critical", "jsonl") => true,
            ("response", "json" or "stage") => true,
            ("session", "open") => true,
            _ => false
        };
    }

    internal static string Folder(string root) => Path.Combine(Path.GetFullPath(root), "logs");
    public static void ValidateDirectory(string directory) => Check(directory);

    internal static void Check(string path)
    {
        for (var entry = new DirectoryInfo(Path.GetFullPath(path)); entry is not null; entry = entry.Parent)
        {
            try
            {
                if ((File.GetAttributes(entry.FullName) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Diagnostic path is redirected.");
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
    }

    internal static string NewName(string folder, string kind, DateTimeOffset at, string extension, string? id = null) =>
        Path.Combine(folder, $"{kind}-{at.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture)}-{id ?? Guid.NewGuid().ToString("N")}.{extension}");

    internal static bool Identify(string path, out string kind, out DateTimeOffset at)
    {
        var match = OwnedName().Match(Path.GetFileName(path));
        kind = match.Groups[1].Value;
        at = default;
        return IsOwned(Path.GetFileName(path)) && DateTimeOffset.TryParseExact(match.Groups[2].Value, TimestampFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal, out at);
    }

    internal static bool Expired(string kind, DateTimeOffset at, DateTimeOffset now) =>
        at > now || (kind == "critical" ? at.AddMonths(1) <= now : at + TimeSpan.FromHours(kind == "trace" ? 72 : 168) <= now);

    internal static IEnumerable<string> Owned(string folder)
    {
        Check(folder);
        if (!Directory.Exists(folder)) return [];
        return Directory.EnumerateFiles(folder).Where(p => IsOwned(Path.GetFileName(p))).ToArray();
    }

    internal static void Prune(string folder, DateTimeOffset now, DiagnosticOptions options, string? reserveKind = null, long reserveBytes = 0, bool onlyReservedKind = false)
    {
        Check(folder);
        if (!Directory.Exists(folder)) return;
        var pattern = onlyReservedKind ? reserveKind + "-*" : "*";
        // Windows directory enumeration already supplies attributes and length. Do not stat every
        // ancestor again for every retained file; validate the exact path again before deletion.
        foreach (var group in new DirectoryInfo(folder).EnumerateFiles(pattern).Where(f => IsOwned(f.Name))
            .GroupBy(f => { Identify(f.Name, out var kind, out _); return kind; }))
        {
            var files = new List<(string Path, DateTimeOffset At, long Size)>();
            foreach (var file in group)
            {
                var path = file.FullName;
                if ((file.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Diagnostic file is redirected.");
                Identify(path, out var kind, out var at);
                if (kind == "session")
                {
                    try { using var lease = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None); }
                    catch (IOException) { continue; }
                }
                if (Expired(kind, at, now)) { Check(path); File.Delete(path); continue; }
                files.Add((path, at, file.Length));
            }
            var budget = group.Key switch
            {
                "trace" => options.TraceBytes, "response" => options.ResponseBytes,
                "critical" => options.CriticalBytes, "session" => 64 * 1024, _ => options.ApplicationBytes - 64 * 1024
            };
            var total = files.Sum(f => f.Size) + (group.Key == reserveKind ? reserveBytes : 0);
            foreach (var file in files.OrderBy(f => f.At))
            {
                if (total <= budget) break;
                Check(file.Path);
                File.Delete(file.Path);
                total -= file.Size;
            }
        }
    }

    public static void DeleteOwned(string ownedRoot)
    {
        var folder = Folder(ownedRoot);
        var paths = Owned(folder).ToArray();
        var legacy = Path.Combine(Path.GetFullPath(ownedRoot), "diagnostics.v1.log");
        // Preflight every entry before deleting any. Unknown entries and directories remain untouched.
        foreach (var path in paths) Check(path);
        Check(legacy);
        foreach (var path in paths) File.Delete(path);
        File.Delete(legacy);
    }
}
