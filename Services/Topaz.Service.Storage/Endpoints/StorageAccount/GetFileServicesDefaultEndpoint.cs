using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.StorageAccount;

/// <summary>
/// Returns a stub response for GET fileServices/default so that the azurerm Terraform provider
/// does not enter an infinite polling loop waiting for this ARM sub-resource to become available.
/// </summary>
internal sealed class GetFileServicesDefaultEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}/fileServices/default"
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
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}/fileServices/default";

        var result = new FileServicesDefaultResponse(id, storageAccountName);
        response.CreateJsonContentResponse(result);
    }
}

internal sealed class FileServicesDefaultResponse(string id, string storageAccountName)
{
    public string Id { get; } = id;
    public string Name { get; } = "default";
    public string Type { get; } = "Microsoft.Storage/storageAccounts/fileServices";

    public FileServicesProperties Properties { get; } = new();

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

internal sealed class FileServicesProperties
{
    public FileServicesShareDeleteRetentionPolicy ShareDeleteRetentionPolicy { get; } = new();
    public FileServicesProtocolSettings ProtocolSettings { get; } = new();
}

internal sealed class FileServicesShareDeleteRetentionPolicy
{
    public bool Enabled { get; } = true;
    public int Days { get; } = 7;
}

internal sealed class FileServicesProtocolSettings
{
    public object Smb { get; } = new { };
}
