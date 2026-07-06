using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Topaz.Chaos;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Host;

internal sealed class GetHealthEndpoint : IEndpointDefinition
{
    public string[] Endpoints => ["GET /health"];
    public string[] Permissions => [];
    public string? ProviderNamespace => "Topaz";

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        response.CreateJsonContentResponse(new HealthResponse(Environment.CurrentDirectory, ThisAssembly.AssemblyInformationalVersion, ChaosStateProvider.IsEnabled));
    }

    private sealed record HealthResponse([UsedImplicitly] string WorkingDirectory, string Version, bool ChaosEnabled = false)
    {
        [UsedImplicitly] public string Status => "Healthy";
        [UsedImplicitly] public string RunningMode => HostState.IsRunningInsideContainer ? "Container" : "Standalone";
        [UsedImplicitly] public bool HttpsConnectProxyAvailable => HostState.HttpsConnectProxyAvailable;
        [UsedImplicitly] public bool AcrDockerExecutorAvailable => HostState.AcrDockerExecutorAvailable;

        public override string ToString() =>
            JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
