using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.StorageAccount;

internal sealed class GetStorageAccountEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly AzureStorageControlPlane _controlPlane = new(new ResourceProvider(logger), logger);

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}"
    ];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/read"];

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

            var storageAccount = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
            if (storageAccount.result == OperationResult.NotFound || storageAccount.resource == null)
            {
                logger.LogInformation($"Storage account [{storageAccountName}] not found.");
                response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode,
                    $"Microsoft.Storage/storageAccounts/{storageAccountName}", resourceGroupIdentifier);
                return;
            }

            response.CreateJsonContentResponse(storageAccount.resource);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
