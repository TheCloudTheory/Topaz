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

internal sealed class UpdatePublicIpAddressTagsEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly PublicIpAddressControlPlane _controlPlane = PublicIpAddressControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.Network";

    public string[] Endpoints =>
    [
        "PATCH /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/publicIPAddresses/{publicIpAddressName}"
    ];

    public string[] Permissions => ["Microsoft.Network/publicIPAddresses/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(UpdatePublicIpAddressTagsEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var name = context.Request.Path.Value.ExtractValueFromPath(8);

            using var reader = new StreamReader(context.Request.Body);
            var content = reader.ReadToEnd();
            var request = JsonSerializer.Deserialize<UpdatePublicIpAddressTagsRequest>(content, GlobalSettings.JsonOptions);

            if (request == null)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            var result = _controlPlane.UpdateTags(subscriptionIdentifier, resourceGroupIdentifier, name!, request);

            if (result.Result == OperationResult.NotFound)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                response.CreateErrorResponse(result.Code!, result.Reason!, HttpStatusCode.NotFound);
                return;
            }

            response.CreateJsonContentResponse(result.Resource!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
