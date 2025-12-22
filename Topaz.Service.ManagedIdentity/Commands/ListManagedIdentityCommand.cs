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
[CommandDefinition("identity list", "managed-identity", "Lists user-assigned managed identities.")]
[CommandExample("Lists managed identities by resource group", "topaz identity list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --resource-group \"rg-local\"")]
[CommandExample("Lists managed identities by subscription", "topaz identity list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae")]
public sealed class ListManagedIdentityCommand(ITopazLogger logger) : Command<ListManagedIdentityCommand.ListManagedIdentityCommandSettings>
{
    public override int Execute(CommandContext context, ListManagedIdentityCommandSettings settings)
    {
        logger.LogInformation("Listing user-assigned managed identities...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        
        var controlPlane = new ManagedIdentityControlPlane(
            new ManagedIdentityResourceProvider(logger),
            new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger),
                new SubscriptionControlPlane(new SubscriptionResourceProvider(logger)), logger),
            new SubscriptionControlPlane(new SubscriptionResourceProvider(logger)), 
            logger);

        if (!string.IsNullOrEmpty(settings.ResourceGroup))
        {
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
            var operation = controlPlane.ListByResourceGroup(subscriptionIdentifier, resourceGroupIdentifier);
            
            if (operation.Result != OperationResult.Success)
            {
                logger.LogError($"({operation.Code}) {operation.Reason}");
                return 1;
            }

            if (operation.Resource == null || operation.Resource.Length == 0)
            {
                logger.LogInformation("No managed identities found in the resource group.");
                return 0;
            }

            foreach (var identity in operation.Resource)
            {
                logger.LogInformation(identity.ToString());
            }
        }
        else
        {
            var operation = controlPlane.ListBySubscription(subscriptionIdentifier);
            
            if (operation.Result != OperationResult.Success)
            {
                logger.LogError($"({operation.Code}) {operation.Reason}");
                return 1;
            }

            if (operation.Resource == null || operation.Resource.Length == 0)
            {
                logger.LogInformation("No managed identities found in the subscription.");
                return 0;
            }

            foreach (var identity in operation.Resource)
            {
                logger.LogInformation(identity.ToString());
            }
        }

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListManagedIdentityCommandSettings settings)
    {
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
    public sealed class ListManagedIdentityCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Optional) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
        
        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
    }
}
