using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Commands;

[UsedImplicitly]
public sealed class DeleteServiceBusQueueCommand(ITopazLogger logger) : Command<DeleteServiceBusQueueCommand.DeleteServiceBusQueueCommandSettings>
{
    public override int Execute(CommandContext context, DeleteServiceBusQueueCommandSettings settings)
    {
        logger.LogDebug($"Executing {nameof(DeleteServiceBusQueueCommand)}.{nameof(Execute)}.");

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

        var controlPlane = new ServiceBusServiceControlPlane(new ResourceProvider(logger), logger);
        var namespaceIdentifier = ServiceBusNamespaceIdentifier.From(settings.NamespaceName!);
        var @namespace = controlPlane.GetNamespace(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier);
        if (@namespace.result == OperationResult.NotFound || @namespace.resource == null)
        {
            logger.LogError($"Namespace {namespaceIdentifier} not found.");
            return 1;
        }
        
        var result = controlPlane.DeleteQueue(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier, settings.Name!);
        if (result == OperationResult.Failed)
        {
            logger.LogError($"There was a problem deleting queue '{settings.Name!}'.");
            return 1;
        }
        
        logger.LogInformation($"Queue '{settings.Name}' deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteServiceBusQueueCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Service Bus queue name can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.NamespaceName))
        {
            return ValidationResult.Error("Service Bus namespace name can't be null.");
        }

        if(string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Service Bus namespace resource group can't be null.");
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
    public sealed class DeleteServiceBusQueueCommandSettings : CommandSettings
    {
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
        
        [CommandOption("-n|--queue-name")]
        public string? Name { get; set; }
        
        [CommandOption("--namespace-name")]
        public string? NamespaceName { get; set; }

        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}