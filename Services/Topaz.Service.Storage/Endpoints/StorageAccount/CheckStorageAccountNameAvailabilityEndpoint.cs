using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.StorageAccount;

internal sealed class CheckStorageAccountNameAvailabilityEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly AzureStorageControlPlane _controlPlane = AzureStorageControlPlane.New(logger);

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/providers/Microsoft.Storage/checkNameAvailability"
    ];

    public string[] Permissions => ["Microsoft.Storage/checkNameAvailability/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));

            using var reader = new StreamReader(context.Request.Body);
            var content = reader.ReadToEnd();
            var request = JsonSerializer.Deserialize<CheckStorageAccountNameAvailabilityRequest>(content,
                GlobalSettings.JsonOptions);

            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            var (_, availability) = _controlPlane.CheckNameAvailability(subscriptionIdentifier, request.Name,
                request.Type);
            response.CreateJsonContentResponse(availability);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.BadRequest;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}