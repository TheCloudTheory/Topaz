using System.Net;
using System.Net.Sockets;
using System.Text;
using Topaz.Shared;

namespace Topaz.Host;

/// <summary>
/// Lightweight HTTP CONNECT proxy that remaps port-443 requests targeting Topaz hostnames
/// to the Topaz resource-manager port (8899), allowing MSAL's user-realm discovery pre-flight
/// to succeed on non-Docker local installs without requiring elevated privileges.
///
/// Routing rules (evaluated in order):
///   1. {TopazHostname}:443 or *.{TopazHostname}:443  →  127.0.0.1:{DefaultResourceManagerPort}
///   2. *.{TopazHostname}:{port}                       →  127.0.0.1:{port}  (direct, non-443)
///   3. Everything else                                →  DNS-resolved host:{port}  (transparent pass-through)
///
/// Set HTTPS_PROXY=http://127.0.0.1:{ConnectProxyPort} in the client environment to use.
/// </summary>
internal sealed class ConnectProxy
{
    private readonly string _bindAddress;
    private readonly ITopazLogger _logger;

    public ConnectProxy(string bindAddress, ITopazLogger logger)
    {
        _bindAddress = bindAddress;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Parse(_bindAddress), GlobalSettings.ConnectProxyPort);
        listener.Start();

        _logger.LogDebug(nameof(ConnectProxy), nameof(RunAsync),
            $"CONNECT proxy listening on {_bindAddress}:{GlobalSettings.ConnectProxyPort}");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                _ = HandleClientAsync(client, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var _ = client;
        try
        {
            var stream = client.GetStream();

            // Read the CONNECT request line (e.g. "CONNECT topaz.local.dev:443 HTTP/1.1")
            var requestLine = await ReadLineAsync(stream, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(requestLine))
                return;

            // Drain remaining headers
            while (true)
            {
                var header = await ReadLineAsync(stream, cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrEmpty(header))
                    break;
            }

            if (!TryParseConnectTarget(requestLine, out var targetHost, out var targetPort))
            {
                _logger.LogDebug(nameof(ConnectProxy), nameof(HandleClientAsync),
                    $"Malformed CONNECT request: {requestLine}");
                await WriteAsync(stream, "HTTP/1.1 400 Bad Request\r\n\r\n", cancellationToken).ConfigureAwait(false);
                return;
            }

            var (resolvedHost, resolvedPort) = ResolveTarget(targetHost, targetPort);

            _logger.LogDebug(nameof(ConnectProxy), nameof(HandleClientAsync),
                $"CONNECT {targetHost}:{targetPort} → {resolvedHost}:{resolvedPort}");

            TcpClient upstream;
            try
            {
                upstream = new TcpClient();
                await upstream.ConnectAsync(resolvedHost, resolvedPort, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(nameof(ConnectProxy), nameof(HandleClientAsync),
                    $"Failed to connect to {resolvedHost}:{resolvedPort}: {ex.Message}");
                await WriteAsync(stream, "HTTP/1.1 502 Bad Gateway\r\n\r\n", cancellationToken).ConfigureAwait(false);
                return;
            }

            await WriteAsync(stream, "HTTP/1.1 200 Connection Established\r\n\r\n", cancellationToken)
                .ConfigureAwait(false);

            using var __ = upstream;
            await PipeAsync(stream, upstream.GetStream(), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(nameof(ConnectProxy), nameof(HandleClientAsync),
                $"Connection error: {ex.Message}");
        }
    }

    private static (string host, int port) ResolveTarget(string requestedHost, int requestedPort)
    {
        var topazSuffix = "." + GlobalSettings.TopazHostname; // ".topaz.local.dev"

        var isTopaz = requestedHost.Equals(GlobalSettings.TopazHostname, StringComparison.OrdinalIgnoreCase)
                      || requestedHost.EndsWith(topazSuffix, StringComparison.OrdinalIgnoreCase);

        if (isTopaz && requestedPort == GlobalSettings.HttpsPort)
        {
            // MSAL user-realm discovery hits :443 — remap to the resource-manager port
            return ("127.0.0.1", GlobalSettings.DefaultResourceManagerPort);
        }

        if (isTopaz)
        {
            // Other Topaz sub-service ports tunnel directly to loopback
            return ("127.0.0.1", requestedPort);
        }

        // Non-Topaz host: pass through transparently
        return (requestedHost, requestedPort);
    }

    private static bool TryParseConnectTarget(string requestLine, out string host, out int port)
    {
        host = string.Empty;
        port = 0;

        // Expected format: "CONNECT host:port HTTP/1.x"
        var parts = requestLine.Split(' ');
        if (parts.Length < 2 || !parts[0].Equals("CONNECT", StringComparison.OrdinalIgnoreCase))
            return false;

        var hostPort = parts[1];
        var lastColon = hostPort.LastIndexOf(':');
        if (lastColon < 0)
            return false;

        host = hostPort[..lastColon];
        return int.TryParse(hostPort[(lastColon + 1)..], out port);
    }

    private static async Task<string> ReadLineAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new List<byte>(256);
        var singleByte = new byte[1];

        while (true)
        {
            var read = await stream.ReadAsync(singleByte, cancellationToken).ConfigureAwait(false);
            if (read == 0) break;

            if (singleByte[0] == '\n')
            {
                // Strip trailing \r if present
                if (buffer.Count > 0 && buffer[^1] == '\r')
                    buffer.RemoveAt(buffer.Count - 1);
                break;
            }

            buffer.Add(singleByte[0]);
        }

        return Encoding.ASCII.GetString([.. buffer]);
    }

    private static Task WriteAsync(NetworkStream stream, string text, CancellationToken cancellationToken)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        return stream.WriteAsync(bytes, cancellationToken).AsTask();
    }

    private static async Task PipeAsync(NetworkStream client, NetworkStream upstream,
        CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var clientToUpstream = CopyAsync(client, upstream, cts);
        var upstreamToClient = CopyAsync(upstream, client, cts);

        await Task.WhenAny(clientToUpstream, upstreamToClient).ConfigureAwait(false);
        await cts.CancelAsync().ConfigureAwait(false);

        await Task.WhenAll(clientToUpstream, upstreamToClient).ConfigureAwait(false);
    }

    private static async Task CopyAsync(NetworkStream source, NetworkStream destination,
        CancellationTokenSource cts)
    {
        var buffer = new byte[8192];
        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var read = await source.ReadAsync(buffer, cts.Token).ConfigureAwait(false);
                if (read == 0) break;
                await destination.WriteAsync(buffer.AsMemory(0, read), cts.Token).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException or SocketException)
        {
            // Either side closed — normal tunnel teardown
        }
        finally
        {
            await cts.CancelAsync().ConfigureAwait(false);
        }
    }
}
