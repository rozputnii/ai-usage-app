using System.Drawing;

namespace AiUsage.Platform;

/// <summary>
/// T-062 R-01: owns the icon the tray shows. H.NotifyIcon's Icon property disposes the shown icon before it updates the
/// shell and ignores a failed update, which leaves the tray holding a destroyed handle that it re-adds after Explorer
/// restarts. Here the shown icon stays alive until a new one is in the tray: a failed or throwing update releases the new
/// icon, a successful one releases the previous icon.
/// </summary>
internal sealed class TrayIconOwner : IDisposable
{
    private Icon? current;

    /// <summary>Puts <paramref name="next"/> in the tray through <paramref name="update"/>; takes ownership of it either way.</summary>
    public bool Show(Icon next, Func<Icon, bool> update)
    {
        bool updated;
        try { updated = update(next); }
        catch
        {
            next.Dispose();
            throw;
        }
        if (!updated)
        {
            next.Dispose();
            return false;
        }
        var previous = current;
        current = next;
        previous?.Dispose();
        return true;
    }

    /// <summary>Releases the shown icon; call it after the tray icon is removed.</summary>
    public void Dispose()
    {
        current?.Dispose();
        current = null;
    }
}
