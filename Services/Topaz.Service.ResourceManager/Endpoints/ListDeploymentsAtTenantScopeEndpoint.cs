using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceManager.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class ListDeploymentsAtTenantScopeEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly TenantDeploymentControlPlane _controlPlane =
        new(new TenantDeploymentResourceProvider(logger), logger);

    public string[] Endpoints =>
    [
        "GET /providers/Microsoft.Resources/deployments",
        "GET /providers/Microsoft.Resources/deployments/"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var result = _controlPlane.List();
        response.CreateJsonContentResponse(new TenantDeploymentListResult(result.Resource!));
    }
}
