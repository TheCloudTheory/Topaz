using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.AppService.Endpoints.Sites;

internal sealed class DeleteAppServiceSiteEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly AppServiceSiteControlPlane _controlPlane = AppServiceSiteControlPlane.New(logger);

    public string? ProviderNamespace => "Microsoft.Web";

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites/{siteName}"
    ];

    public string[] Permissions => ["Microsoft.Web/sites/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(DeleteAppServiceSiteEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var siteName = context.Request.Path.Value.ExtractValueFromPath(8);

            if (string.IsNullOrWhiteSpace(siteName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            var existing = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, siteName);
            switch (existing.Result)
            {
                case OperationResult.NotFound:
                    response.StatusCode = HttpStatusCode.NotFound;
                    return;
                case OperationResult.Failed:
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return;
                default:
                    _controlPlane.Delete(subscriptionIdentifier, resourceGroupIdentifier, siteName);
                    response.StatusCode = HttpStatusCode.OK;
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
