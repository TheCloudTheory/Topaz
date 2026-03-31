using JetBrains.Annotations;
using Topaz.Documentation.Command;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;

namespace Topaz.Service.ResourceGroup.Commands;

[UsedImplicitly]
[CommandDefinition("group delete", "group", "Deletes a resource group.")]
[CommandExample("Delete a resource group", "topaz group delete \\\n    --name \"my-rg\" \\\n    --subscription-id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\"")]
public sealed class DeleteResourceGroupCommand(Pipeline eventPipeline, ITopazLogger logger) : Command<DeleteResourceGroupCommand.DeleteResourceGroupCommandSettings>
{
    public override int Execute(CommandContext context, DeleteResourceGroupCommandSettings settings)
    {
        logger.LogDebug(nameof(DeleteResourceGroupCommand), nameof(Execute), "Executing {0}.{1}.", nameof(DeleteResourceGroupCommand), nameof(Execute));
        logger.LogInformation("Deleting resource group...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.Name!);
        var controlPlane = new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);
        var existingResource = controlPlane.Get(SubscriptionIdentifier.From(settings.SubscriptionId), resourceGroupIdentifier);
        if (existingResource.Result == OperationResult.NotFound)
        {
            logger.LogError($"Resource group '{settings.Name}' could not be found.");
            return 1;
        }
        
        _= controlPlane.Delete(subscriptionIdentifier, resourceGroupIdentifier);
        
        logger.LogInformation("Resource group deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteResourceGroupCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Resource group name can't be null.");
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
    public sealed class DeleteResourceGroupCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
    }
}
