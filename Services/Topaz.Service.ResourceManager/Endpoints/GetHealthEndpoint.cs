using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Endpoints;

internal sealed class GetHealthEndpoint : IEndpointDefinition
{
    public string[] Endpoints => ["GET /health"];
    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        response.CreateJsonContentResponse(new HealthResponse(Environment.CurrentDirectory));
    }

    private sealed record HealthResponse(string WorkingDirectory)
    {
        public string Status => "Healthy";

        public override string ToString() =>
            JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
