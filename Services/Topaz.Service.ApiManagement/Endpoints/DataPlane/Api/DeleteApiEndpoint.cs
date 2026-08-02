using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ApiManagement.Endpoints.DataPlane.Api;

internal sealed class DeleteApiEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ApiManagementApiControlPlane _controlPlane =
        ApiManagementApiControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.ApiManagement";

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ApiManagement/service/{serviceName}/apis/{apiId}"
    ];

    public string[] Permissions => ["Microsoft.ApiManagement/service/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var rg = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var name = context.Request.Path.Value.ExtractValueFromPath(8);
        var apiId = context.Request.Path.Value.ExtractValueFromPath(10);
        var deleteRevisions = context.Request.Query.ContainsKey("deleteRevisions") &&  context.Request.Query["deleteRevisions"] == "true";

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(apiId))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var ifMatch = context.Request.Headers.TryGetValue("If-Match", out var value)
            ? value.ToString()
            : null;

        var result = _controlPlane.Delete(sub, rg, name, apiId, ifMatch, deleteRevisions);
        switch (result.Result)
        {
            case OperationResult.NotFound:
                response.CreateNotFoundResponse(result);
                return;
            case OperationResult.BadRequest:
                response.CreateErrorResponse(result, HttpStatusCode.BadRequest);
                return;
            case OperationResult.Conflict:
                response.CreateErrorResponse(result, HttpStatusCode.Conflict);
                return;
        }

        if (result.Result != OperationResult.Deleted)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!);
            return;
        }
        
        response.CreateNoContentResponse();
    }
}