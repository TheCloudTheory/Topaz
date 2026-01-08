using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.ResourceGroup;
using Topaz.Service.ServiceBus.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Commands;

[UsedImplicitly]
public class CreateServiceBusQueueCommand(ITopazLogger logger) : Command<CreateServiceBusQueueCommand.CreateServiceBusQueueCommandSettings>
{
    public override int Execute(CommandContext context, CreateServiceBusQueueCommandSettings settings)
    {
        logger.LogDebug($"Executing {nameof(CreateServiceBusQueueCommand)}.{nameof(Execute)}.");

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

        var controlPlane = new ServiceBusServiceControlPlane(new ServiceBusResourceProvider(logger), logger);
        var namespaceIdentifier = ServiceBusNamespaceIdentifier.From(settings.NamespaceName!);
        var @namespace = controlPlane.GetNamespace(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier);
        if (@namespace.result == OperationResult.NotFound || @namespace.resource == null)
        {
            logger.LogError($"Namespace {namespaceIdentifier} not found.");
            return 1;
        }
        
        var queue = controlPlane.CreateOrUpdateQueue(resourceGroup.Resource.GetSubscription(), resourceGroupIdentifier, namespaceIdentifier, settings.Name!, new CreateOrUpdateServiceBusQueueRequest());
        if (queue.Result == OperationResult.Failed || queue.Resource == null)
        {
            logger.LogError($"There was a problem creating queue '{settings.Name!}'.");
            return 1;
        }
        
        logger.LogInformation(queue.Resource.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateServiceBusQueueCommandSettings settings)
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
            return ValidationResult.Error("Service Bus subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Service Bus subscription ID must be a valid GUID.");
        }

        return base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class CreateServiceBusQueueCommandSettings : CommandSettings
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