using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AiUsage.Infrastructure.Providers;

/// <summary>
/// Minimal loopback-only HTTP receiver for the browser sign-in redirect. A raw socket bound to
/// 127.0.0.1 is used instead of HTTP.sys, which reserves a wildcard endpoint and would accept
/// off-machine requests carrying a loopback Host header.
/// </summary>
internal sealed class LoopbackCallback : IDisposable
{
    private const int MaximumRequestBytes = 8 * 1024;
    private readonly TcpListener[] listeners;
    private readonly Task<TcpClient>?[] pending;
    private readonly CancellationTokenSource lifetime = new();

    private LoopbackCallback(TcpListener[] listeners, int port)
    {
        this.listeners = listeners;
        pending = new Task<TcpClient>?[listeners.Length];
        Port = port;
    }

    internal int Port { get; }

    internal static LoopbackCallback Start(IEnumerable<int> ports)
    {
        foreach (var port in ports)
        {
            // `localhost` resolves to either loopback family on Windows. The IPv4 endpoint is
            // required because the fixed redirect is tried there first; IPv6 is bound when available.
            // Neither endpoint accepts off-machine traffic.
            var bound = new List<TcpListener>(2);
            var actualPort = port;
            foreach (var address in new[] { IPAddress.Loopback, IPAddress.IPv6Loopback })
            {
                var candidate = new TcpListener(address, actualPort);
                try
                {
                    candidate.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
                    if (address.AddressFamily == AddressFamily.InterNetworkV6)
                        candidate.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, true);
                    candidate.Start(1);
                    bound.Add(candidate);
                    actualPort = ((IPEndPoint)candidate.LocalEndpoint).Port;
                }
                catch (SocketException) { candidate.Dispose(); }
                catch (PlatformNotSupportedException) { candidate.Dispose(); }
            }
            if (bound.Count > 0 && bound[0].LocalEndpoint is IPEndPoint { AddressFamily: AddressFamily.InterNetwork })
                return new LoopbackCallback([.. bound], actualPort);
            foreach (var listener in bound)
                listener.Dispose();
        }
        throw new IOException("The local sign-in callback is unavailable.");
    }

    /// <summary>Accepts one browser request and returns its request target, or null when unreadable.</summary>
    internal async Task<CallbackRequest?> AcceptAsync(CancellationToken cancellationToken)
    {
        // Accepts live for the whole attempt, not one call: a request arriving on the other loopback
        // family while this one is handled stays queued, and caller cancellation never poisons them.
        for (var index = 0; index < listeners.Length; index++)
            pending[index] ??= listeners[index].AcceptTcpClientAsync(lifetime.Token).AsTask();
        var accepted = await Task.WhenAny(pending!).WaitAsync(cancellationToken).ConfigureAwait(false);
        var slot = Array.IndexOf(pending, accepted);
        var acceptTask = pending[slot]!;
        pending[slot] = null;
        var client = await acceptTask.ConfigureAwait(false);
        var stream = client.GetStream();
        var handedOff = false;
        using var headerDeadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        headerDeadline.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            var buffer = new byte[MaximumRequestBytes];
            var read = 0;
            while (read < buffer.Length)
            {
                var chunk = await stream.ReadAsync(buffer.AsMemory(read), headerDeadline.Token).ConfigureAwait(false);
                if (chunk == 0)
                    break;
                read += chunk;
                var text = Encoding.ASCII.GetString(buffer, 0, read);
                var headerEnd = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                if (headerEnd < 0)
                    continue;
                var line = text[..text.IndexOf("\r\n", StringComparison.Ordinal)].Split(' ');
                if (line.Length != 3 || !StringComparer.Ordinal.Equals(line[0], "GET"))
                    break;
                handedOff = true;
                return new CallbackRequest(client, stream, line[1]);
            }
        }
        catch (IOException) { }
        catch (SocketException) { }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && headerDeadline.IsCancellationRequested) { }
        finally
        {
            if (!handedOff)
            {
                stream.Dispose();
                client.Dispose();
            }
        }
        return null;
    }

    public void Dispose()
    {
        lifetime.Cancel();
        foreach (var listener in listeners)
            listener.Dispose();
        // Observe the retained accepts so a queued socket is closed and no fault is left unhandled.
        foreach (var accept in pending.Where(accept => accept is not null))
            _ = accept!.ContinueWith(static task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                    task.Result.Dispose();
                else
                    _ = task.Exception;
            }, TaskScheduler.Default);
        lifetime.Dispose();
    }
}

/// <summary>One accepted browser redirect. The response is written after the caller decides its outcome.</summary>
internal sealed class CallbackRequest(TcpClient client, NetworkStream stream, string target) : IDisposable
{
    internal string Target { get; } = target;

    internal async Task RespondAsync(HttpStatusCode status, string message, CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetBytes($"<!doctype html><meta charset=\"utf-8\"><title>AI Usage</title><p>{message}</p>");
        var head = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {(int)status} {status}\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {body.Length}\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n");
        try
        {
            await stream.WriteAsync(head, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(body, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        // The browser page is a courtesy; a failed write must not change the authentication outcome.
        catch (IOException) { }
        catch (SocketException) { }
        catch (ObjectDisposedException) { }
        catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        stream.Dispose();
        client.Dispose();
    }
}
