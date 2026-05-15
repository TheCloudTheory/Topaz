using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.VirtualNetwork.Models.Requests;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.VirtualNetwork.Endpoints;

internal sealed class CreateOrUpdateNetworkInterfaceEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly NetworkInterfaceControlPlane _controlPlane = NetworkInterfaceControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.Network";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/networkInterfaces/{networkInterfaceName}"
    ];

    public string[] Permissions => ["Microsoft.Network/networkInterfaces/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(CreateOrUpdateNetworkInterfaceEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var name = context.Request.Path.Value.ExtractValueFromPath(8);

            if (string.IsNullOrWhiteSpace(name))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            using var reader = new StreamReader(context.Request.Body);
            var content = reader.ReadToEnd();
            var request = JsonSerializer.Deserialize<CreateOrUpdateNetworkInterfaceRequest>(content, GlobalSettings.JsonOptions)!;

            var operation = _controlPlane.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, name, request);

            if (operation.Result == OperationResult.NotFound)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                response.CreateErrorResponse(operation.Code!, operation.Reason!, HttpStatusCode.NotFound);
                return;
            }

            response.StatusCode = operation.Result == OperationResult.Created ? HttpStatusCode.Created : HttpStatusCode.OK;
            response.CreateJsonContentResponse(operation.Resource!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
