namespace AiUsage.Presentation.Tests;

internal static class Repository
{
    public static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CONTRIBUTING.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Run from a repository build output.");
    }
}


