using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.EventHub.Models.Requests;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Commands;

[UsedImplicitly]
public sealed class CreateEventHubCommand(ITopazLogger logger) : Command<CreateEventHubCommand.CreateEventHubCommandSettings>
{
    public override int Execute(CommandContext context, CreateEventHubCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var resourceGroupControlPlane =
            new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), logger);
        var resourceGroup = resourceGroupControlPlane.Get(SubscriptionIdentifier.From(settings.SubscriptionId), resourceGroupIdentifier);
        if (resourceGroup.result == OperationResult.NotFound || resourceGroup.resource == null)
        {
            logger.LogError($"Resource group {resourceGroupIdentifier} not found.");
            return 1;
        }
        
        var controlPlane = new EventHubServiceControlPlane(new ResourceProvider(logger), logger);
        var namespaceIdentifier = EventHubNamespaceIdentifier.From(settings.NamespaceName!);
        var @namespace = controlPlane.GetNamespace(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier);
        if (@namespace.result == OperationResult.NotFound || @namespace.resource == null)
        {
            logger.LogError($"Namespace {namespaceIdentifier} not found.");
            return 1;
        }

        var queue = controlPlane.CreateOrUpdateEventHub(resourceGroup.resource.GetSubscription(),
            resourceGroupIdentifier, namespaceIdentifier, settings.Name!, new CreateOrUpdateEventHubRequest());
        if (queue.result == OperationResult.Failed || queue.resource == null)
        {
            logger.LogError($"There was a problem creating queue '{settings.Name!}'.");
            return 1;
        }
        
        logger.LogInformation(queue.resource.ToString());

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
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
        
        [CommandOption("--namespace-name")]
        public string? NamespaceName { get; set; }
        
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
        
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
    }
}