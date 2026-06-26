using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.AppService.Endpoints.Sites;

internal sealed class GetSlotConfigNamesEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly AppServiceSiteControlPlane _controlPlane = AppServiceSiteControlPlane.New(logger);

    public string? ProviderNamespace => "Microsoft.Web";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites/{siteName}/config/slotConfigNames"
    ];

    public string[] Permissions => ["Microsoft.Web/sites/config/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(GetSlotConfigNamesEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var siteName = context.Request.Path.Value.ExtractValueFromPath(8);

            var result = _controlPlane.GetSlotConfigNames(subscriptionIdentifier, resourceGroupIdentifier, siteName!);
            if (result.Result == OperationResult.NotFound || result.Resource == null)
            {
                response.CreateErrorResponse(result.Code!, result.Reason!, HttpStatusCode.NotFound);
                return;
            }

            response.CreateJsonContentResponse(result.Resource);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
