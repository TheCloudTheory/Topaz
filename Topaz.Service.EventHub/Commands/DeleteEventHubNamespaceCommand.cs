using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Commands;

[UsedImplicitly]
public class DeleteEventHubNamespaceCommand(ITopazLogger logger) : Command<DeleteEventHubNamespaceCommand.DeleteEventHubNamespaceCommandSettings>
{
    public override int Execute(CommandContext context, DeleteEventHubNamespaceCommandSettings settings)
    {
        logger.LogInformation("Deleting Azure Event Hub Namespace...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var rp = new ResourceProvider(logger);
        
        rp.Delete(subscriptionIdentifier, resourceGroupIdentifier, settings.Name!);

        logger.LogInformation("Azure Event Hub Namespace deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteEventHubNamespaceCommandSettings settings)
    {
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
        
        return string.IsNullOrEmpty(settings.Name) 
            ? ValidationResult.Error("Azure Event Hub Namespace name can't be null.") : base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class DeleteEventHubNamespaceCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
        
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
        
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
    }
}