using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers;

/// <summary>Reject redirected roots and exact owned paths before reading, replacing or deleting.</summary>
internal static class ProviderStatePaths
{
    internal static void CheckDirectory(string path)
    {
        for (var current = new DirectoryInfo(path); current is not null; current = current.Parent)
        {
            try
            {
                var attributes = File.GetAttributes(current.FullName);
                if ((attributes & FileAttributes.ReparsePoint) != 0 || (attributes & FileAttributes.Directory) == 0)
                    throw new ProviderException(ProviderFailureKind.RecoveryRequired);
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
    }

    internal static void CheckFile(string path)
    {
        try
        {
            if ((File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
                throw new ProviderException(ProviderFailureKind.RecoveryRequired);
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
    }

    internal static FileStream Acquire(string directory, string lockName)
    {
        CheckDirectory(directory);
        Directory.CreateDirectory(directory);
        CheckDirectory(directory);
        var lockPath = Path.Combine(directory, lockName);
        CheckFile(lockPath);
        return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
    }
}
