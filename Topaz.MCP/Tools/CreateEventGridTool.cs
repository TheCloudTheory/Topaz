using System.ComponentModel;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.EventGrid;
using Azure.ResourceManager.EventGrid.Models;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Creates Event Grid resources in a running Topaz instance.")]
[UsedImplicitly]
public sealed class CreateEventGridTool
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [McpServerTool]
    [Description("Creates an Event Grid topic in the given resource group and returns the endpoint URL and relevant metadata.")]
    [UsedImplicitly]
    public static async Task<EventGridTopicResult> CreateEventGridTopic(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group where the store will be created.")]
        string resourceGroupName,
        [Description("Name of the Event Grid topic to create.")]
        string topicName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId,
        [Description("Event input schema. Can be CustomEventSchema | EventGridSchema | CloudEventSchemaV1_0")]
        string inputSchema = "EventGridSchema")
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);

        var data = new EventGridTopicData(new AzureLocation(location))
        {
            InputSchema = new EventGridInputSchema(inputSchema)
        };
        
        var operation = await resourceGroup.Value.GetEventGridTopics()
            .CreateOrUpdateAsync(WaitUntil.Completed, topicName, data)
            .ConfigureAwait(false);

        var topic = operation.Value;
        var sharedAccessKey = await topic.GetSharedAccessKeysAsync();

        return new EventGridTopicResult
        {
            Name = topic.Data.Name,
            ResourceId = topic.Data.Id?.ToString(),
            Endpoint = GlobalSettings.GetEventGridEndpoint(topicName, subscriptionId.ToString()),
            Key1 = sharedAccessKey.Value.Key1,
            Key2 = sharedAccessKey.Value.Key2,
            ProvisioningState = topic.Data.ProvisioningState?.ToString()
        };
    }
    
    [McpServerTool]
    [Description("Creates an Event Grid subscription in the given resource group and returns the endpoint URL and relevant metadata.")]
    [UsedImplicitly]
    public static async Task<EventGridSubscriptionResult> CreateEventGridSubscriptionForWebhook(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group where the store will be created.")]
        string resourceGroupName,
        [Description("Name of the Event Grid topic for a subscription.")]
        string topicName,
        [Description("Name of the Event Grid subscription to created.")]
        string topicSubscriptionName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId,
        [Description("URL of the webhook used for the event subscription.")]
        string webhookUrl)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);

        var data = new EventGridSubscriptionData()
        {
            Destination = new WebHookEventSubscriptionDestination()
            {
                Endpoint = new Uri(webhookUrl)
            }
        };
        var operation = await (await resourceGroup.Value.GetEventGridTopicAsync(topicName))
            .Value.GetTopicEventSubscriptions()
            .CreateOrUpdateAsync(WaitUntil.Completed, topicSubscriptionName, data)
            .ConfigureAwait(false);

        var eventSubscription = operation.Value;

        return new EventGridSubscriptionResult
        {
            Name = eventSubscription.Data.Name,
            ResourceId = eventSubscription.Data.Id?.ToString(),
            Endpoint = GlobalSettings.GetEventGridEndpoint(topicName, subscriptionId.ToString()),
            ProvisioningState = eventSubscription.Data.ProvisioningState?.ToString()
        };
    }

    public sealed record EventGridTopicResult
    {
        public required string? Name { [UsedImplicitly] get; init; }
        public required string? ResourceId { [UsedImplicitly] get; init; }
        public required string Endpoint { [UsedImplicitly] get; init; }
        public required string Key1 { [UsedImplicitly] get; init; }
        public required string Key2 { [UsedImplicitly] get; init; }
        public required string? ProvisioningState { [UsedImplicitly] get; init; }
    }
    
    public sealed record EventGridSubscriptionResult
    {
        public required string? Name { [UsedImplicitly] get; init; }
        public required string? ResourceId { [UsedImplicitly] get; init; }
        public required string Endpoint { [UsedImplicitly] get; init; }
        public required string? ProvisioningState { [UsedImplicitly] get; init; }
    }
}
