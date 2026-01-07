using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.EventHub.Models.Requests;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Commands;

[UsedImplicitly]
[CommandDefinition("eventhubs eventhub create",  "event-hub", "Creates new Event Hub.")]
[CommandExample("Creates Event Hub", "topaz eventhubs eventhub create \\\n    --resource-group rg-test \\\n    --namespace-name \"eh-namespace\" \\\n    --name \"hubtest\" \\\n    --subscription-id \"07CB2605-9C16-46E9-A2BD-0A8D39E049E8\"")]
public sealed class CreateEventHubCommand(ITopazLogger logger) : Command<CreateEventHubCommand.CreateEventHubCommandSettings>
{
    public override int Execute(CommandContext context, CreateEventHubCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var resourceGroupControlPlane =
            new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), new SubscriptionControlPlane(new SubscriptionResourceProvider(logger)), logger);
        var resourceGroup = resourceGroupControlPlane.Get(SubscriptionIdentifier.From(settings.SubscriptionId), resourceGroupIdentifier);
        if (resourceGroup.Result == OperationResult.NotFound || resourceGroup.Resource == null)
        {
            logger.LogError($"Resource group {resourceGroupIdentifier} not found.");
            return 1;
        }
        
        var controlPlane = new EventHubServiceControlPlane(new EventHubResourceProvider(logger), logger);
        var namespaceIdentifier = EventHubNamespaceIdentifier.From(settings.NamespaceName!);
        var @namespace = controlPlane.GetNamespace(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier);
        if (@namespace.Result == OperationResult.NotFound || @namespace.Resource == null)
        {
            logger.LogError($"Namespace {namespaceIdentifier} not found.");
            return 1;
        }

        var queue = controlPlane.CreateOrUpdateEventHub(resourceGroup.Resource.GetSubscription(),
            resourceGroupIdentifier, namespaceIdentifier, settings.Name!, new CreateOrUpdateEventHubRequest());
        if (queue.Result == OperationResult.Failed || queue.Resource == null)
        {
            logger.LogError($"There was a problem creating queue '{settings.Name!}'.");
            return 1;
        }
        
        logger.LogInformation(queue.Resource.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateEventHubCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Event Hub hub name can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.NamespaceName))
        {
            return ValidationResult.Error("Event Hub namespace name can't be null.");
        }

        if(string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Event Hub namespace resource group can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Resource group subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Resource group subscription ID must be a valid GUID.");
        }

        return base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class CreateEventHubCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) hub name.")]
        [CommandOption("-n|--name")] public string? Name { get; set; }
        
        [CommandOptionDefinition("(Required) namespace name.")]
        [CommandOption("--namespace-name")]
        public string? NamespaceName { get; set; }
        
        [CommandOptionDefinition("(Required) resource group name.")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
        
        [CommandOptionDefinition("(Required) subscription ID.")]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
    }
}