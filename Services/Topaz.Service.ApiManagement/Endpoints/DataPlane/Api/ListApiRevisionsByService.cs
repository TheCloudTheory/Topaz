using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ApiManagement.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ApiManagement.Endpoints.DataPlane.Api;

internal sealed class ListApiRevisionsByService(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ApiManagementApiControlPlane _controlPlane =
        ApiManagementApiControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.ApiManagement";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ApiManagement/service/{serviceName}/apis/{apiId}/revisions"
    ];

    public string[] Permissions => ["Microsoft.ApiManagement/service/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var rg = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var name = context.Request.Path.Value.ExtractValueFromPath(8);
        var apiId = context.Request.Path.Value.ExtractValueFromPath(10);

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(apiId))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var result = _controlPlane.ListRevisionsByService(sub, rg, name, apiId);
        if (result.Result == OperationResult.NotFound)
        {
            response.CreateNotFoundResponse(result);
            return;
        }

        if (result.Result != OperationResult.Success || result.Resource == null)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!);
            return;
        }

        response.CreateJsonContentResponse(ApiManagementListApiRevisionsResponse.From(result.Resource));
    }
}