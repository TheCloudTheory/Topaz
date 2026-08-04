using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ApiManagement.Endpoints.DataPlane.Product;

internal sealed class DeleteProductApiEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ApiManagementProductControlPlane _controlPlane =
        ApiManagementProductControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.ApiManagement";

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ApiManagement/service/{serviceName}/products/{productId}/apis/{apiId}"
    ];

    public string[] Permissions => ["Microsoft.ApiManagement/service/products/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var rg = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var name = context.Request.Path.Value.ExtractValueFromPath(8);
        var productId = context.Request.Path.Value.ExtractValueFromPath(10);
        var apiId = context.Request.Path.Value.ExtractValueFromPath(12);

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(apiId))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var result = _controlPlane.DeleteProductApi(sub, rg, name, productId, apiId);
        if (result.Result == OperationResult.NotFound)
        {
            response.CreateNotFoundResponse(result);
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