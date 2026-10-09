using Xunit;

namespace AiUsage.Windows.Tests;

public sealed class AuditDesktopPrerequisite
{
    [Fact(Explicit = true)]
    public void UnlockedInputDesktopIsAvailable() => DesktopTestEnvironment.RequireUnlockedDesktop();
}
