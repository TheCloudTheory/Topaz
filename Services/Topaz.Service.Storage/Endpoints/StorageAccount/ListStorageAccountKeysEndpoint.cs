using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models.Responses;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.StorageAccount;

internal sealed class ListStorageAccountKeysEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly AzureStorageControlPlane _controlPlane = new(new StorageResourceProvider(logger), logger);

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}/listKeys"
    ];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/listKeys/action"];

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
            if (storageAccount.Result == OperationResult.NotFound || storageAccount.Resource == null)
            {
                logger.LogInformation($"Storage account [{storageAccountName}] not found.");
                response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode,
                    $"Microsoft.Storage/storageAccounts/{storageAccountName}", resourceGroupIdentifier);
                return;
            }

            var keys = new ListKeysResponse(storageAccount.Resource.Keys);
            response.CreateJsonContentResponse(keys);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
