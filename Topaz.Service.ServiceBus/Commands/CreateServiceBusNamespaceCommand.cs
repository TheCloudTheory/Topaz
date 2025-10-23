using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.ResourceGroup;
using Topaz.Service.ServiceBus.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Commands;

[UsedImplicitly]
public sealed class CreateServiceBusNamespaceCommand(ITopazLogger logger) : Command<CreateServiceBusNamespaceCommand.CreateServiceBusNamespaceCommandSettings>
{
    public override int Execute(CommandContext context, CreateServiceBusNamespaceCommandSettings settings)
    {
        logger.LogDebug($"Executing {nameof(CreateServiceBusNamespaceCommand)}.{nameof(Execute)}.");

        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var resourceGroupControlPlane =
            new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), logger);
        var resourceGroup = resourceGroupControlPlane.Get(SubscriptionIdentifier.From(settings.SubscriptionId), resourceGroupIdentifier);
        if (resourceGroup.result == OperationResult.NotFound || resourceGroup.resource == null)
        {
            logger.LogError($"ResourceGroup {resourceGroupIdentifier} not found.");
            return 1;
        }

        var namespaceIdentifier = ServiceBusNamespaceIdentifier.From(settings.Name!);
        var controlPlane = new ServiceBusServiceControlPlane(new ResourceProvider(logger), logger);
        var request = new CreateOrUpdateServiceBusNamespaceRequest();
        var ns = controlPlane.CreateOrUpdateNamespace(resourceGroup.resource.GetSubscription(), resourceGroupIdentifier, resourceGroup.resource.Location, namespaceIdentifier, request);

        if (ns.result == OperationResult.Failed || ns.resource == null)
        {
            logger.LogError($"There was a problem creating namespace '{namespaceIdentifier}'.");
            return 1;
        }
        
        logger.LogInformation(ns.resource.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateServiceBusNamespaceCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
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
    public sealed class CreateServiceBusNamespaceCommandSettings : CommandSettings
    {
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
        
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}