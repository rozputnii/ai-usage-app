using AiUsage.Core.Diagnostics;
using AiUsage.Infrastructure.Diagnostics;

// Dedicated disposable process: test runners may intercept unhandled exceptions before our handler.
if (args is not [var root, var mode] || mode is not ("crash" or "wait") ||
    !Path.GetFullPath(root).StartsWith(Path.Combine(Path.GetTempPath(), "aiu-crash-"), StringComparison.OrdinalIgnoreCase)) return 2;
using var log = new FileDiagnostics(root);
AppDomain.CurrentDomain.UnhandledException += (_, incident) => log.Fatal(DiagnosticEvent.UnhandledFailure, incident.ExceptionObject as Exception, incident.IsTerminating);
await log.FlushAsync();
if (mode == "wait") await Task.Delay(Timeout.InfiniteTimeSpan);
var crashing = new Thread(() =>
{
    for (var i = 0; i < 20000; i++) log.Signal(DiagnosticEvent.OperationStarted);
    throw new InvalidOperationException("crash-canary");
});
crashing.Start();
crashing.Join();
return 0;
