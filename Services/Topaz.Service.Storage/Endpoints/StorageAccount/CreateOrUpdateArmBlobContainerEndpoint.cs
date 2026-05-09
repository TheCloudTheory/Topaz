using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.StorageAccount;

internal sealed class CreateOrUpdateArmBlobContainerEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly BlobServiceControlPlane _controlPlane = new(new BlobResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.Storage";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}/blobServices/default/containers/{containerName}"
    ];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/blobServices/containers/write"];

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

        var result = _controlPlane.CreateContainer(subscriptionIdentifier, resourceGroupIdentifier, containerName!,
            storageAccountName!);

        var statusCode = result.Result == OperationResult.Conflict ? HttpStatusCode.OK : HttpStatusCode.Created;

        var armResponse = new ArmBlobContainerResponse(subscriptionId!, resourceGroupName!, storageAccountName!, containerName!);
        response.StatusCode = statusCode;
        response.CreateJsonContentResponse(armResponse);
    }
}

internal sealed class ArmBlobContainerResponse(string subscriptionId, string resourceGroupName, string storageAccountName, string containerName)
{
    public string Id { get; } =
        $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}/blobServices/default/containers/{containerName}";
    public string Name { get; } = containerName;
    public string Type { get; } = "Microsoft.Storage/storageAccounts/blobServices/containers";
    public ArmBlobContainerProperties Properties { get; } = new();

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

internal sealed class ArmBlobContainerProperties
{
    public string PublicAccess { get; } = "None";
    public bool HasImmutabilityPolicy { get; } = false;
    public bool HasLegalHold { get; } = false;
    public string LeaseState { get; } = "Available";
    public string LeaseStatus { get; } = "Unlocked";
}
