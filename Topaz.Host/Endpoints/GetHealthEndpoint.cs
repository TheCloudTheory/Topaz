using Microsoft.AspNetCore.Http;
using Topaz.Chaos;
using Topaz.Host.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Host.Endpoints;

internal sealed class GetHealthEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["GET /health"];
    public string[] Permissions => [];
    public string ProviderNamespace => "Topaz";

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var healthResponse = new HealthResponse(new HostState(logger), Environment.CurrentDirectory, ThisAssembly.AssemblyInformationalVersion, ChaosStateProvider.IsEnabled);
        response.CreateJsonContentResponse(healthResponse);
    }
}
