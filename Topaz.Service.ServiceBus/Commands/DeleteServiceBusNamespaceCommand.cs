using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Commands;

[UsedImplicitly]
public sealed class DeleteServiceBusNamespaceCommand(ITopazLogger logger) : Command<DeleteServiceBusNamespaceCommand.DeleteServiceBusNamespaceCommandSettings>
{
    public override int Execute(CommandContext context, DeleteServiceBusNamespaceCommandSettings settings)
    {
        logger.LogDebug($"Executing {nameof(DeleteServiceBusNamespaceCommand)}.{nameof(Execute)}.");
        
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var controlPlane = new ServiceBusServiceControlPlane(new ServiceBusResourceProvider(logger), logger);
        _ = controlPlane.DeleteNamespace(subscriptionIdentifier, resourceGroupIdentifier, settings.Name!);

        logger.LogInformation("Service Bus namespace deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteServiceBusNamespaceCommandSettings settings)
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
            return ValidationResult.Error("Service Bus subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Service Bus subscription ID must be a valid GUID.");
        }

        return base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class DeleteServiceBusNamespaceCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
        
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
        
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
    }
}