using JetBrains.Annotations;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;

namespace Topaz.Service.ResourceGroup.Commands;

[UsedImplicitly]
public sealed class DeleteResourceGroupCommand(ITopazLogger logger) : Command<DeleteResourceGroupCommand.DeleteResourceGroupCommandSettings>
{
    public override int Execute(CommandContext context, DeleteResourceGroupCommandSettings settings)
    {
        logger.LogDebug(nameof(DeleteResourceGroupCommand), nameof(Execute), "Executing {0}.{1}.", nameof(DeleteResourceGroupCommand), nameof(Execute));
        logger.LogInformation("Deleting resource group...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.Name!);
        var controlPlane = new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), new SubscriptionControlPlane(new SubscriptionResourceProvider(logger)), logger);
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
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
        
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
    }
}
