using Microsoft.AspNetCore.Http;
using Topaz.Chaos;
using Topaz.Host.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Host;

internal sealed class GetHealthEndpoint : IEndpointDefinition
{
    public string[] Endpoints => ["GET /health"];
    public string[] Permissions => [];
    public string ProviderNamespace => "Topaz";

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var healthResponse = new HealthResponse(Environment.CurrentDirectory, ThisAssembly.AssemblyInformationalVersion, ChaosStateProvider.IsEnabled);
        response.CreateJsonContentResponse(healthResponse);
    }
}
