using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.StorageAccount;

internal sealed class CreateOrUpdateStorageAccountEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly AzureStorageControlPlane _controlPlane = new(new ResourceProvider(logger), logger);

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}"
    ];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var storageAccountName = context.Request.Path.Value.ExtractValueFromPath(8);

            if (string.IsNullOrWhiteSpace(storageAccountName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            using var reader = new StreamReader(context.Request.Body);
            var content = reader.ReadToEnd();
            var request = JsonSerializer.Deserialize<CreateOrUpdateStorageAccountRequest>(content, GlobalSettings.JsonOptions);

            if (request == null)
            {
                logger.LogError($"Could not deserialize the request content of {content}.");
                response.StatusCode = HttpStatusCode.InternalServerError;
                return;
            }

            var result = _controlPlane.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, request);
            response.CreateJsonContentResponse(result.Resource);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
