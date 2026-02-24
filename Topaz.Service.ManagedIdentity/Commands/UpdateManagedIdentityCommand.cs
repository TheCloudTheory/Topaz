using JetBrains.Annotations;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.ManagedIdentity.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ManagedIdentity.Commands;

[UsedImplicitly]
[CommandDefinition("identity update", "managed-identity", "Updates a user-assigned managed identity.")]
[CommandExample("Updates a managed identity with tags", "topaz identity update --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"myIdentity\" \\\n    --resource-group \"rg-local\" \\\n    --tags environment=production team=devops")]
public sealed class UpdateManagedIdentityCommand(ITopazLogger logger) : Command<UpdateManagedIdentityCommand.UpdateManagedIdentityCommandSettings>
{
    public override int Execute(CommandContext context, UpdateManagedIdentityCommandSettings settings)
    {
        logger.LogInformation("Updating user-assigned managed identity...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var managedIdentityIdentifier = ManagedIdentityIdentifier.From(settings.Name!);
        var controlPlane = ManagedIdentityControlPlane.New(logger);

        var existingIdentity = controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityIdentifier);
        if (existingIdentity.Resource == null)
        {
            logger.LogError($"Managed identity '{settings.Name}' not found in resource group '{settings.ResourceGroup}'.");
            return 1;
        }

        var request = new CreateUpdateManagedIdentityRequest
        {
            Location = existingIdentity.Resource.Location,
            Tags = settings.Tags?.ToDictionary(t => t.Split('=')[0], t => t.Split('=')[1]),
            Properties = new CreateUpdateManagedIdentityRequest.ManagedIdentityProperties
            {
                IsolationScope = settings.IsolationScope ?? existingIdentity.Resource.Properties.IsolationScope
            }
        };

        var operation = controlPlane.Update(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityIdentifier, request);
        
        if (operation.Result != OperationResult.Updated)
        {
            logger.LogError($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        logger.LogInformation(operation.Resource!.ToString());
        logger.LogInformation("User-assigned managed identity updated.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, UpdateManagedIdentityCommandSettings settings)
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
    public sealed class UpdateManagedIdentityCommandSettings : CommandSettings
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

        [CommandOptionDefinition("(Optional) resource tags")]
        [CommandOption("--tags")]
        public string[]? Tags { get; set; }

        [CommandOptionDefinition("(Optional) isolation scope (None or Regional)")]
        [CommandOption("--isolation-scope")]
        public string? IsolationScope { get; set; }
    }
}
