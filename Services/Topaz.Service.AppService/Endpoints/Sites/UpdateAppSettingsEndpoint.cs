using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.AppService.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.AppService.Endpoints.Sites;

internal sealed class UpdateAppSettingsEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly AppServiceSiteControlPlane _controlPlane = AppServiceSiteControlPlane.New(logger);

    public string? ProviderNamespace => "Microsoft.Web";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites/{siteName}/config/appsettings"
    ];

    public string[] Permissions => ["Microsoft.Web/sites/config/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public async void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(UpdateAppSettingsEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var siteName = context.Request.Path.Value.ExtractValueFromPath(8);

            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<UpdateAppSettingsRequest>(body, GlobalSettings.JsonOptions)
                          ?? new UpdateAppSettingsRequest();

            var result = _controlPlane.UpdateAppSettings(
                subscriptionIdentifier,
                resourceGroupIdentifier,
                siteName!,
                request.Properties ?? new Dictionary<string, string>());

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
