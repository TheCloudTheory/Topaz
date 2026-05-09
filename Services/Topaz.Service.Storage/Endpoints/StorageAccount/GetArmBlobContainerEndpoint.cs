using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.StorageAccount;

internal sealed class GetArmBlobContainerEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly BlobServiceControlPlane _controlPlane = new(new BlobResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.Storage";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}/blobServices/default/containers/{containerName}"
    ];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/blobServices/containers/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionId = context.Request.Path.Value.ExtractValueFromPath(2);
        var resourceGroupName = context.Request.Path.Value.ExtractValueFromPath(4);
        var storageAccountName = context.Request.Path.Value.ExtractValueFromPath(8);
        var containerName = context.Request.Path.Value.ExtractValueFromPath(12);

        var subscriptionIdentifier = SubscriptionIdentifier.From(subscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(resourceGroupName);

        var (exists, _) = _controlPlane.GetContainerMetadataState(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName!, containerName!);

        if (!exists)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode,
                $"Microsoft.Storage/storageAccounts/blobServices/containers/{containerName}", resourceGroupIdentifier);
            return;
        }

        var armResponse = new ArmBlobContainerResponse(subscriptionId!, resourceGroupName!, storageAccountName!, containerName!);
        response.StatusCode = HttpStatusCode.OK;
        response.CreateJsonContentResponse(armResponse);
    }
}
