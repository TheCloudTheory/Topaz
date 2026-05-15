using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.VirtualNetwork.Endpoints;

internal sealed class DeletePublicIpAddressEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly PublicIpAddressControlPlane _controlPlane = PublicIpAddressControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.Network";

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/publicIPAddresses/{publicIpAddressName}"
    ];

    public string[] Permissions => ["Microsoft.Network/publicIPAddresses/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(DeletePublicIpAddressEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

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

            var deleteResult = _controlPlane.Delete(subscriptionIdentifier, resourceGroupIdentifier, name);
            if (deleteResult.Result == OperationResult.NotFound)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                response.CreateErrorResponse(deleteResult.Code!, deleteResult.Reason!, HttpStatusCode.NotFound);
                return;
            }

            response.StatusCode = HttpStatusCode.OK;
            response.Content = new ByteArrayContent([]);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
