using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.StorageAccount;

internal sealed class CreateOrUpdateArmQueueEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly QueueServiceControlPlane _controlPlane = QueueServiceControlPlane.New(logger);

    public string? ProviderNamespace => "Microsoft.Storage";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}/queueServices/default/queues/{queueName}"
    ];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/queueServices/queues/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionId = context.Request.Path.Value.ExtractValueFromPath(2);
        var resourceGroupName = context.Request.Path.Value.ExtractValueFromPath(4);
        var storageAccountName = context.Request.Path.Value.ExtractValueFromPath(8);
        var queueName = context.Request.Path.Value.ExtractValueFromPath(12);

        var subscriptionIdentifier = SubscriptionIdentifier.From(subscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(resourceGroupName);

        var alreadyExists = _controlPlane.QueueExists(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);

        if (!alreadyExists)
        {
            _controlPlane.CreateQueue(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName);
        }

        var statusCode = alreadyExists ? HttpStatusCode.OK : HttpStatusCode.Created;

        var armResponse = new ArmQueueResponse(subscriptionId!, resourceGroupName!, storageAccountName!, queueName!);
        response.StatusCode = statusCode;
        response.CreateJsonContentResponse(armResponse);
    }
}

internal sealed class ArmQueueResponse(string subscriptionId, string resourceGroupName, string storageAccountName, string queueName)
{
    public string Id { get; } =
        $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}/queueServices/default/queues/{queueName}";
    public string Name { get; } = queueName;
    public string Type { get; } = "Microsoft.Storage/storageAccounts/queueServices/queues";
    public ArmQueueProperties Properties { get; } = new();

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

internal sealed class ArmQueueProperties
{
    public object Metadata { get; } = new { };
    public int ApproximateMessageCount { get; } = 0;
}
