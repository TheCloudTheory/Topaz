using System.ComponentModel;
using System.Net.Sockets;
using System.Text.Json;
using JetBrains.Annotations;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Topaz.Shared;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Returns the current status of a running Topaz instance.")]
[UsedImplicitly]
public sealed class StatusTool
{
    private static readonly HttpClient HttpClient = new();

    private static readonly IReadOnlyList<(string Name, ushort Port)> KnownServices =
    [
        ("Resource Manager",     GlobalSettings.DefaultResourceManagerPort),
        ("Key Vault",            GlobalSettings.DefaultKeyVaultPort),
        ("Blob Storage",         GlobalSettings.DefaultBlobStoragePort),
        ("Queue Storage",        GlobalSettings.DefaultQueueStoragePort),
        ("Table Storage",        GlobalSettings.DefaultTableStoragePort),
        ("File Storage",         GlobalSettings.DefaultFileStoragePort),
        ("Container Registry",   GlobalSettings.ContainerRegistryPort),
        ("Event Hub (HTTP)",     GlobalSettings.DefaultEventHubPort),
        ("Event Hub (AMQP)",     GlobalSettings.DefaultEventHubAmqpPort),
        ("Service Bus (AMQP)",   GlobalSettings.DefaultServiceBusAmqpPort),
        ("Service Bus (Extra)",  GlobalSettings.AdditionalServiceBusPort),
    ];

    [McpServerTool]
    [Description("Calls the Topaz health-check endpoint and probes all service ports. Returns the running version, overall status, working directory, and which services are up. Useful for debugging when a setup fails partway through.")]
    [UsedImplicitly]
    public static async Task<TopazStatusResult> GetTopazStatus()
    {
        var healthUrl = $"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}/health";

        HttpResponseMessage httpResponse;
        try
        {
            httpResponse = await HttpClient.GetAsync(healthUrl).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or SocketException)
        {
            throw new McpException(
                $"Topaz host is not reachable at {healthUrl}. Start it first with 'topaz-host start'.",
                ex);
        }

        var json = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var version          = root.TryGetProperty("version",          out var v)  ? v.GetString()  ?? "Unknown" : "Unknown";
        var status           = root.TryGetProperty("status",           out var s)  ? s.GetString()  ?? "Unknown" : "Unknown";
        var workingDirectory = root.TryGetProperty("workingDirectory", out var wd) ? wd.GetString() ?? "Unknown" : "Unknown";

        var serviceStatuses = await Task.WhenAll(
            KnownServices.Select(svc => ProbePortAsync(svc.Name, svc.Port))
        ).ConfigureAwait(false);

        return new TopazStatusResult
        {
            Version          = version,
            Status           = status,
            WorkingDirectory = workingDirectory,
            Services         = [.. serviceStatuses],
        };
    }

    private static async Task<ServiceStatus> ProbePortAsync(string name, ushort port)
    {
        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
            await socket.ConnectAsync("topaz.local.dev", port, cts.Token).ConfigureAwait(false);
            return new ServiceStatus { Name = name, Port = port, IsUp = true };
        }
        catch
        {
            return new ServiceStatus { Name = name, Port = port, IsUp = false };
        }
    }

    public sealed record TopazStatusResult
    {
        public required string Version          { [UsedImplicitly] get; init; }
        public required string Status           { [UsedImplicitly] get; init; }
        public required string WorkingDirectory { [UsedImplicitly] get; init; }
        public required List<ServiceStatus> Services { [UsedImplicitly] get; init; }
    }

    public sealed record ServiceStatus
    {
        public required string Name  { [UsedImplicitly] get; init; }
        public required ushort Port  { [UsedImplicitly] get; init; }
        public required bool   IsUp  { [UsedImplicitly] get; init; }
    }
}

