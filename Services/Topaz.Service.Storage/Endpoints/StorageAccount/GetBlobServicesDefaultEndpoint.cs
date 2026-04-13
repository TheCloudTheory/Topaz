using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.StorageAccount;

/// <summary>
/// Returns a stub response for GET blobServices/default so that the azurerm Terraform provider
/// does not fail with 404 when reading blob properties after creating a storage account.
/// </summary>
internal sealed class GetBlobServicesDefaultEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}/blobServices/default"
    ];

    public string[] Permissions => ["*"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionId = context.Request.Path.Value.ExtractValueFromPath(2);
        var resourceGroupName = context.Request.Path.Value.ExtractValueFromPath(4);
        var storageAccountName = context.Request.Path.Value.ExtractValueFromPath(8);

        var id =
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}/blobServices/default";

        var result = new BlobServicesDefaultResponse(id);
        response.CreateJsonContentResponse(result);
    }
}

internal sealed class BlobServicesDefaultResponse(string id)
{
    public string Id { get; } = id;
    public string Name { get; } = "default";
    public string Type { get; } = "Microsoft.Storage/storageAccounts/blobServices";
    public BlobServicesProperties Properties { get; } = new();

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

internal sealed class BlobServicesProperties
{
    public BlobServicesDeleteRetentionPolicy DeleteRetentionPolicy { get; } = new();
    public BlobServicesDeleteRetentionPolicy ContainerDeleteRetentionPolicy { get; } = new();
    public bool IsVersioningEnabled { get; } = false;
    public BlobServicesCorsRules Cors { get; } = new();
}

internal sealed class BlobServicesDeleteRetentionPolicy
{
    public bool Enabled { get; } = false;
    public int Days { get; } = 7;
}

internal sealed class BlobServicesCorsRules
{
    public object[] CorsRules { get; } = [];
}
