using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.AppService.Endpoints.Sites;

internal sealed class ListAppServiceSitesByResourceGroupEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly AppServiceSiteControlPlane _controlPlane = AppServiceSiteControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Web";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites"
    ];

    public string[] Permissions => ["Microsoft.Web/sites/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListAppServiceSitesByResourceGroupEndpoint), nameof(GetResponse), "Executing {0}.",
            nameof(GetResponse));

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));

            var sites = _controlPlane.ListByResourceGroup(subscriptionIdentifier, resourceGroupIdentifier);
            if (sites.Result != OperationResult.Success || sites.Resource == null)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                return;
            }

            var result = new ListSubscriptionResourcesResponse
            {
                Value = sites.Resource.Select(ListSubscriptionResourcesResponse.GenericResourceExpanded.From!).ToArray()
            };

            response.CreateJsonContentResponse(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
