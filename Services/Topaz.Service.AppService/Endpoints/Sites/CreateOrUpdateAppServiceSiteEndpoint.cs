using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.AppService.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.AppService.Endpoints.Sites;

internal sealed class CreateOrUpdateAppServiceSiteEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly AppServiceSiteControlPlane _controlPlane = AppServiceSiteControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Web";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites/{siteName}"
    ];

    public string[] Permissions => ["Microsoft.Web/sites/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(CreateOrUpdateAppServiceSiteEndpoint), nameof(GetResponse), "Executing {0}.",
            nameof(GetResponse));

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

            using var reader = new StreamReader(context.Request.Body);
            var content = reader.ReadToEnd();
            logger.LogDebug(nameof(CreateOrUpdateAppServiceSiteEndpoint), nameof(GetResponse),
                "Processing payload: {0}", content);

            var request = JsonSerializer.Deserialize<CreateOrUpdateAppServiceSiteRequest>(content, GlobalSettings.JsonOptions);
            if (request == null)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                return;
            }

            var result = _controlPlane.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, siteName, request);
            if ((result.Result != OperationResult.Created && result.Result != OperationResult.Updated) || result.Resource == null)
            {
                response.CreateErrorResponse(result.Code!, result.Reason!);
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
