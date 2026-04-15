using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.StorageAccount;

internal sealed class CreateOrUpdateArmTableEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly TableServiceControlPlane _controlPlane = new(new TableResourceProvider(logger), logger);

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}/tableServices/default/tables/{tableName}"
    ];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/tableServices/tables/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionId = context.Request.Path.Value.ExtractValueFromPath(2);
        var resourceGroupName = context.Request.Path.Value.ExtractValueFromPath(4);
        var storageAccountName = context.Request.Path.Value.ExtractValueFromPath(8);
        var tableName = context.Request.Path.Value.ExtractValueFromPath(12);

        var subscriptionIdentifier = SubscriptionIdentifier.From(subscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(resourceGroupName);

        var tableExists = _controlPlane.CheckIfTableExists(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, tableName);

        if (!tableExists)
        {
            _controlPlane.CreateTable(subscriptionIdentifier, resourceGroupIdentifier, tableName, storageAccountName);
            // Result discarded intentionally — existence check above handles the create/skip logic
        }

        var id =
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}/tableServices/default/tables/{tableName}";

        var result = new ArmTableResponse(id, tableName);
        response.StatusCode = HttpStatusCode.OK;
        response.CreateJsonContentResponse(result);
    }
}

internal sealed class ArmTableResponse(string id, string name)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public string Type { get; } = "Microsoft.Storage/storageAccounts/tableServices/tables";
    public ArmTableProperties Properties { get; } = new(name);

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

internal sealed class ArmTableProperties(string tableName)
{
    public string TableName { get; } = tableName;
}
