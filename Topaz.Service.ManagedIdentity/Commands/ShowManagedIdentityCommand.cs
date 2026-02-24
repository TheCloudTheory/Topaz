using JetBrains.Annotations;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ManagedIdentity.Commands;

[UsedImplicitly]
[CommandDefinition("identity show", "managed-identity", "Shows details of a user-assigned managed identity.")]
[CommandExample("Shows a managed identity", "topaz identity show --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"myIdentity\" \\\n    --resource-group \"rg-local\"")]
public sealed class ShowManagedIdentityCommand(ITopazLogger logger) : Command<ShowManagedIdentityCommand.ShowManagedIdentityCommandSettings>
{
    public override int Execute(CommandContext context, ShowManagedIdentityCommandSettings settings)
    {
        logger.LogInformation("Retrieving user-assigned managed identity...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var managedIdentityIdentifier = ManagedIdentityIdentifier.From(settings.Name!);
        var controlPlane = ManagedIdentityControlPlane.New(logger);
        
        var operation = controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityIdentifier);
        
        if (operation.Resource == null)
        {
            logger.LogError($"Managed identity '{settings.Name}' not found in resource group '{settings.ResourceGroup}'.");
            return 1;
        }

        logger.LogInformation(operation.Resource.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ShowManagedIdentityCommandSettings settings)
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
    public sealed class ShowManagedIdentityCommandSettings : CommandSettings
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
