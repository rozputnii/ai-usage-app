using AiUsage.Core.Providers.Codex;
using System.Net;
using System.Text;
using AiUsage.Infrastructure.Providers.Codex;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

// DPAPI protection is a Windows boundary; the product ships Windows-only.
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class CodexGrantStoreTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "aiusage-grant-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void StoredGrantRoundTripsWithoutPlaintextOnDisk()
    {
        var store = new CodexGrantStore(root);
        Assert.Null(store.Read());

        store.Write(new CodexStoredGrant("synthetic-workspace", "synthetic-refresh-secret"));
        var restored = store.Read();

        Assert.Equal("synthetic-workspace", restored!.AccountId);
        Assert.Equal("synthetic-refresh-secret", restored.RefreshToken);
        Assert.DoesNotContain("synthetic-refresh-secret", restored.ToString());

        var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
        Assert.Single(files);
        foreach (var file in files)
        {
            Assert.DoesNotContain("synthetic-refresh-secret", Path.GetFileName(file));
            var bytes = File.ReadAllBytes(file);
            Assert.DoesNotContain("synthetic-refresh-secret", Encoding.UTF8.GetString(bytes));
            Assert.DoesNotContain("synthetic-workspace", Encoding.UTF8.GetString(bytes));
            Assert.DoesNotContain("synthetic-refresh-secret", Encoding.Unicode.GetString(bytes));
        }
    }

    [Fact]
    public void ReplacingAGrantKeepsExactlyOneRecord()
    {
        var store = new CodexGrantStore(root);
        store.Write(new CodexStoredGrant("synthetic-workspace", "synthetic-first"));
        store.Write(new CodexStoredGrant("synthetic-workspace", "synthetic-second"));

        Assert.Equal("synthetic-second", store.Read()!.RefreshToken);
        Assert.Single(Directory.GetFiles(root));
    }

    [Fact]
    public void DisconnectRemovesTheGrantAndTouchesNothingElse()
    {
        var store = new CodexGrantStore(root);
        var neighbour = Path.Combine(root, "unrelated.txt");
        store.Write(new CodexStoredGrant("synthetic-workspace", "synthetic-refresh"));
        File.WriteAllText(neighbour, "owner data");

        store.Delete();

        Assert.Null(store.Read());
        Assert.True(File.Exists(neighbour));
        store.Delete();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-protected-record")]
    public void UnusableRecordReadsAsAbsentRatherThanFailing(string content)
    {
        var store = new CodexGrantStore(root);
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "codex.grant"), content);
        Assert.Null(store.Read());
    }

    [Fact]
    public async Task ResumeRejectsAGrantRecordThatNamesAnotherWorkspace()
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json(CodexTestServer.Tokens())));
        using var http = new HttpClient(server);
        var error = await Assert.ThrowsAsync<CodexException>(() => new CodexAuthClient(http, new CodexTestServer.Clock())
            .ResumeAsync(new CodexStoredGrant("other-workspace", "synthetic-refresh"), TestContext.Current.CancellationToken));
        Assert.Equal(CodexFailureKind.AccountMismatch, error.Kind);
    }

    [Fact]
    public async Task ResumeRestoresTheStoredWorkspaceAndItsRotatedGrant()
    {
        using var server = new CodexTestServer(async (request, cancellation) =>
        {
            Assert.Contains("synthetic-stored-refresh", await request.Content!.ReadAsStringAsync(cancellation));
            return CodexTestServer.Json(CodexTestServer.Tokens());
        });
        using var http = new HttpClient(server);
        using var credentials = await new CodexAuthClient(http, new CodexTestServer.Clock())
            .ResumeAsync(new CodexStoredGrant("synthetic-workspace", "synthetic-stored-refresh"), TestContext.Current.CancellationToken);
        Assert.Equal("synthetic-workspace", credentials.AccountId);
        Assert.False(credentials.RequiresReauthentication);
    }

    [Fact]
    public async Task AnInvalidatedGrantRequiresANewSignIn()
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(
            CodexTestServer.Json("""{"error":"invalid_grant"}""", HttpStatusCode.BadRequest)));
        using var http = new HttpClient(server);
        var error = await Assert.ThrowsAsync<CodexException>(() => new CodexAuthClient(http, new CodexTestServer.Clock())
            .ResumeAsync(new CodexStoredGrant("synthetic-workspace", "synthetic-stale"), TestContext.Current.CancellationToken));
        Assert.Equal(CodexFailureKind.AuthenticationRequired, error.Kind);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}
