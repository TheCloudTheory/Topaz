using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.StorageAccount;

internal sealed class DeleteArmTableEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly TableServiceControlPlane _controlPlane = new(new TableResourceProvider(logger), logger);

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}/tableServices/default/tables/{tableName}"
    ];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/tableServices/tables/delete"];

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

        _controlPlane.DeleteTable(subscriptionIdentifier, resourceGroupIdentifier, tableName, storageAccountName);

        response.StatusCode = HttpStatusCode.NoContent;
    }
}
