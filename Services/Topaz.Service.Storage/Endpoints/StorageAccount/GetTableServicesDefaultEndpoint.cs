using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.StorageAccount;

/// <summary>
/// Returns a stub response for GET tableServices/default so that the azurerm Terraform provider
/// does not fail with 404 when reading table service properties after creating a storage account.
/// </summary>
internal sealed class GetTableServicesDefaultEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}/tableServices/default"
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
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}/tableServices/default";

        var result = new TableServicesDefaultResponse(id);
        response.CreateJsonContentResponse(result);
    }
}

internal sealed class TableServicesDefaultResponse(string id)
{
    public string Id { get; } = id;
    public string Name { get; } = "default";
    public string Type { get; } = "Microsoft.Storage/storageAccounts/tableServices";
    public TableServicesProperties Properties { get; } = new();

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

internal sealed class TableServicesProperties
{
    public TableServicesCorsRules Cors { get; } = new();
}

internal sealed class TableServicesCorsRules
{
    public object[] CorsRules { get; } = [];
}
