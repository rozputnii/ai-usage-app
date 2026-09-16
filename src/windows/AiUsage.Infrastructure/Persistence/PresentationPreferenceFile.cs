using System.Text;

namespace AiUsage.Infrastructure.Persistence;

/// <summary>Presentation metadata only; separate from provider grants and quota caches.</summary>
public sealed class PresentationPreferenceFile(string ownedDirectory)
{
    private const int MaximumBytes = 256 * 1024;
    private string FilePath => Path.Combine(ownedDirectory, "appearance.v1.json");
    public async Task<string?> ReadAsync(CancellationToken token)
    {
        CheckPath();
        if (!File.Exists(FilePath)) return null;
        if (new FileInfo(FilePath).Length > MaximumBytes) throw new IOException("Preferences are too large.");
        return await File.ReadAllTextAsync(FilePath, token).ConfigureAwait(false);
    }
    public async Task WriteAsync(string content, CancellationToken token)
    {
        if (Encoding.UTF8.GetByteCount(content) > MaximumBytes) throw new IOException("Preferences are too large.");
        CheckPath();
        Directory.CreateDirectory(ownedDirectory);
        var staged = Path.Combine(ownedDirectory, "appearance-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await using (var stream = new FileStream(staged, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(Encoding.UTF8.GetBytes(content), token).ConfigureAwait(false);
                await stream.FlushAsync(token).ConfigureAwait(false);
            }
            token.ThrowIfCancellationRequested();
            CheckPath();
            File.Move(staged, FilePath, overwrite: true);
        }
        finally { if (File.Exists(staged)) File.Delete(staged); }
    }
    private void CheckPath()
    {
        for (var directory = new DirectoryInfo(Path.GetFullPath(ownedDirectory)); directory is not null; directory = directory.Parent)
            if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Preferences path is redirected.");
        if (File.Exists(FilePath) && (File.GetAttributes(FilePath) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Preferences file is redirected.");
    }
}
