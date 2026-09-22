using AiUsage.Core.Diagnostics;
using System.Globalization;
using System.Text;

namespace AiUsage.Infrastructure.Persistence;

public sealed class LocalDiagnosticSink : IDiagnosticSink
{
    private const int MaximumBytes = 64 * 1024;
    private readonly string directory;
    private readonly TimeProvider clock;

    public LocalDiagnosticSink(string ownedDirectory, TimeProvider? clock = null)
    {
        directory = ownedDirectory;
        this.clock = clock ?? TimeProvider.System;
        Rewrite(null);
    }

    public void Record(DiagnosticEvent eventCode, DiagnosticCategory category)
    {
        if (!Enum.IsDefined(eventCode) || !Enum.IsDefined(category)) return;
        Rewrite((eventCode, category));
    }

    private void Rewrite((DiagnosticEvent Event, DiagnosticCategory Category)? next)
    {
        try
        {
            var root = Path.GetFullPath(directory);
            var path = Path.Combine(root, "diagnostics.v1.log");
            CheckPath(root, path);
            if (next is null && !File.Exists(path)) return;
            Directory.CreateDirectory(root);
            CheckPath(root, path);
            using var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var now = clock.GetUtcNow();
            var records = new Queue<string>();
            var size = 0;
            if (file.Length <= MaximumBytes)
            {
                using var reader = new StreamReader(file, Encoding.UTF8, false, 1024, leaveOpen: true);
                while (reader.ReadLine() is { } line)
                {
                    var fields = line.Split('\t');
                    if (fields.Length != 3 ||
                        !DateTimeOffset.TryParseExact(fields[0], "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var at) ||
                        at < now - TimeSpan.FromDays(7) || at > now ||
                        !Enum.TryParse<DiagnosticEvent>(fields[1], out var eventCode) || !Enum.IsDefined(eventCode) ||
                        !Enum.TryParse<DiagnosticCategory>(fields[2], out var category) || !Enum.IsDefined(category)) continue;
                    Add(at, eventCode, category);
                }
            }
            if (next is { } item) Add(now, item.Event, item.Category);
            var bytes = Encoding.UTF8.GetBytes(string.Concat(records));
            file.Position = 0;
            file.Write(bytes);
            file.SetLength(bytes.Length);
            file.Flush();

            void Add(DateTimeOffset at, DiagnosticEvent eventCode, DiagnosticCategory category)
            {
                // Rebuild rather than copying input: every output field has passed the allowlist.
                var record = $"{at.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)}\t{eventCode}\t{category}\n";
                records.Enqueue(record);
                size += record.Length; // All generated characters are ASCII.
                while (size > MaximumBytes) size -= records.Dequeue().Length;
            }
        }
        catch (Exception)
        {
            // Diagnostics must never replace the original fault, recurse into logging or prevent exit.
        }
    }

    private static void CheckPath(string root, string path)
    {
        for (var parent = new DirectoryInfo(root); parent is not null; parent = parent.Parent)
            if ((parent.Attributes & FileAttributes.ReparsePoint) != 0 && parent.Attributes != (FileAttributes)(-1))
                throw new IOException("Diagnostic directory is redirected.");
        // GetAttributes also detects a dangling file link; File.Exists alone does not.
        try
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Diagnostic file is redirected.");
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
    }
}
