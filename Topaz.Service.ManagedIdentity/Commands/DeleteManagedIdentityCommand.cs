using JetBrains.Annotations;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;

namespace Topaz.Service.ManagedIdentity.Commands;

[UsedImplicitly]
[CommandDefinition("identity delete", "managed-identity", "Deletes a user-assigned managed identity.")]
[CommandExample("Deletes a managed identity", "topaz identity delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"myIdentity\" \\\n    --resource-group \"rg-local\"")]
public sealed class DeleteManagedIdentityCommand(ITopazLogger logger) : Command<DeleteManagedIdentityCommand.DeleteManagedIdentityCommandSettings>
{
    public override int Execute(CommandContext context, DeleteManagedIdentityCommandSettings settings)
    {
        logger.LogInformation("Deleting user-assigned managed identity...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var managedIdentityIdentifier = ManagedIdentityIdentifier.From(settings.Name!);
        
        var controlPlane = new ManagedIdentityControlPlane(
            new ManagedIdentityResourceProvider(logger),
            new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger),
                new SubscriptionControlPlane(new SubscriptionResourceProvider(logger)), logger),
            new SubscriptionControlPlane(new SubscriptionResourceProvider(logger)), 
            logger);
        
        var operation = controlPlane.Delete(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityIdentifier);
        
        if (operation.Result == OperationResult.NotFound)
        {
            logger.LogError($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        logger.LogInformation("User-assigned managed identity deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteManagedIdentityCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Managed identity name can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Resource group name can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        }

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class DeleteManagedIdentityCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) managed identity name")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
        
        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
        
        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
    }
}
