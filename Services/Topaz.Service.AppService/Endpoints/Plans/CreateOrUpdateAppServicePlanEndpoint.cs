using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.AppService.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.AppService.Endpoints.Plans;

internal sealed class CreateOrUpdateAppServicePlanEndpoint(ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly AppServicePlanControlPlane _controlPlane = AppServicePlanControlPlane.New(logger);

    public string? ProviderNamespace => "Microsoft.Web";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/serverfarms/{name}"
    ];

    public string[] Permissions => ["Microsoft.Web/serverfarms/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(CreateOrUpdateAppServicePlanEndpoint), nameof(GetResponse), "Executing {0}.",
            nameof(GetResponse));

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var planName = context.Request.Path.Value.ExtractValueFromPath(8);

            if (string.IsNullOrWhiteSpace(planName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            using var reader = new StreamReader(context.Request.Body);
            var content = reader.ReadToEnd();
            logger.LogDebug(nameof(CreateOrUpdateAppServicePlanEndpoint), nameof(GetResponse),
                "Processing payload: {0}", content);

            var request = JsonSerializer.Deserialize<CreateOrUpdateAppServicePlanRequest>(content, GlobalSettings.JsonOptions);
            if (request == null)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                return;
            }

            var result = _controlPlane.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, planName, request);
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
