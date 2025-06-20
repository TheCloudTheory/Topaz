using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Commands;

[UsedImplicitly]
public sealed class CreateServiceBusNamespaceCommand(ITopazLogger logger) : Command<CreateServiceBusNamespaceCommand.CreateServiceBusCommandSettings>
{
    public override int Execute(CommandContext context, CreateServiceBusCommandSettings settings)
    {
        logger.LogDebug($"Executing {nameof(CreateServiceBusNamespaceCommand)}.{nameof(Execute)}.");

        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var resourceGroupControlPlane =
            new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), logger);
        var resourceGroup = resourceGroupControlPlane.Get(resourceGroupIdentifier);
        if (resourceGroup.result == OperationResult.NotFound || resourceGroup.resource == null)
        {
            logger.LogError($"ResourceGroup {resourceGroupIdentifier} not found.");
            return 1;
        }

        var controlPlane = new ServiceBusServiceControlPlane(new ResourceProvider(logger), logger);
        var ns = controlPlane.CreateOrUpdateNamespace(resourceGroup.resource.GetSubscription(), resourceGroupIdentifier, resourceGroup.resource.Location, settings.Name!);

        logger.LogInformation(ns.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateServiceBusCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Service Bus namespace name can't be null.");
        }

        if(string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Service Bus namespace resource group can't be null.");
        }

        return base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class CreateServiceBusCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}