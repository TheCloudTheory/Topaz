using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Authorization.Domain;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Authorization.Endpoints.RoleDefinitions;

internal sealed class GetByIdRoleDefinitionEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly AuthorizationControlPlane _controlPlane = AuthorizationControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "GET /providers/Microsoft.Authorization/roleDefinitions/{roleDefinitionId}"
    ];

    public string[] Permissions => ["Microsoft.Authorization/roleDefinitions/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var roleDefinitionIdentifier =
            RoleDefinitionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));

        logger.LogDebug(nameof(GetByIdRoleDefinitionEndpoint), nameof(GetResponse),
            "Looking for built-in role definition `{0}` by id.", roleDefinitionIdentifier);

        var operation = _controlPlane.GetBuiltInRoleById(roleDefinitionIdentifier);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.CreateJsonContentResponse(operation.Resource);
    }
}
