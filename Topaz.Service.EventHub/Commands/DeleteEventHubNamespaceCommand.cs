using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Commands;

[UsedImplicitly]
[CommandDefinition("eventhubs namespace delete",  "event-hub", "Deletes an Event Hub.")]
[CommandExample("Deletes Event Hub", "topaz eventhubs namespace delete \\\n    --name \"sb-namespace\" \\\n    --resource-group \"rg\" \\\n    --subscription-id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\"")]
public class DeleteEventHubNamespaceCommand(ITopazLogger logger) : Command<DeleteEventHubNamespaceCommand.DeleteEventHubNamespaceCommandSettings>
{
    public override int Execute(CommandContext context, DeleteEventHubNamespaceCommandSettings settings)
    {
        logger.LogInformation("Deleting Azure Event Hub Namespace...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var rp = new EventHubResourceProvider(logger);
        
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
        [CommandOptionDefinition("(Required) Event Hub namespace name.")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
        
        [CommandOptionDefinition("(Required) Event Hub namespace resource group name.")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
        
        [CommandOptionDefinition("(Required) Event Hub namespace subscription ID.")]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
    }
}