using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ResourceManager.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class ListResourceProvidersByTenantEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ResourceManagerControlPlane _controlPlane = ResourceManagerControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "GET /providers"
    ];

    public string[] Permissions => ["*"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var providers = _controlPlane.GetAllProviders();

        response.CreateJsonContentResponse(new ListResourceProvidersResponse
        {
            Value = providers.Resource!.Select(p => new ResourceProviderDataResponse(p)).ToArray()
        });
    }
}