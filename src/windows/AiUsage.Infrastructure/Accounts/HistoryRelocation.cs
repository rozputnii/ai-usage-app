using AiUsage.Core.Diagnostics;
using AiUsage.Infrastructure.Providers;

namespace AiUsage.Infrastructure.Accounts;

/// <summary>
/// AIU-047 startup step under the maintenance root lease, after the account migration: moves the
/// state root's reading store to the history root once (one same-volume rename, never a copy or
/// delete) and refreshes the identity map from the registry. A failed move leaves both roots as
/// they were and surfaces as interrupted maintenance.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class HistoryRelocation(string stateRoot, string historyRoot, AccountRegistry registry,
    AccountIdentityMap identities, IDiagnosticSink? diagnostics = null)
{
    internal async Task RunAsync(CancellationToken token)
    {
        var source = Path.Combine(Path.GetFullPath(stateRoot), "budget");
        var target = Path.Combine(Path.GetFullPath(historyRoot), "budget");
        if (!string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
            await Task.Run(() => Relocate(source, target), token).ConfigureAwait(false);
        var state = await registry.ReadAsync(token).ConfigureAwait(false);
        await identities.UpsertAsync(state.Accounts.ToArray(), token).ConfigureAwait(false);
    }

    private void Relocate(string source, string target)
    {
        Check(source); Check(target);
        if (!Directory.Exists(source)) return;
        if (Directory.Exists(target))
        {
            // History that survived a reinstall wins; the older copy stays for manual recovery.
            diagnostics?.Signal(DiagnosticEvent.HistoryLeftInPlace, DiagnosticSeverity.Warning);
            return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        Check(target);
        Directory.Move(source, target);
        diagnostics?.Signal(DiagnosticEvent.HistoryRelocated);
    }

    private static void Check(string directory)
    {
        try { ProviderStatePaths.CheckDirectory(directory); }
        catch (ProviderException) { throw new IOException("The history location is redirected or not a directory; data is preserved."); }
    }
}
