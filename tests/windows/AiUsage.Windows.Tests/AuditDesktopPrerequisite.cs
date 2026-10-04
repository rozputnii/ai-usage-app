using Xunit;

namespace AiUsage.Windows.Tests;

public sealed class AuditDesktopPrerequisite
{
    [Fact]
    public void UnlockedInputDesktopIsAvailable() => DesktopTestEnvironment.RequireUnlockedDesktop();
}
