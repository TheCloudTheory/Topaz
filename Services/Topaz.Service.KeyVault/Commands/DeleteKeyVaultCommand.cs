using JetBrains.Annotations;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;

namespace Topaz.Service.KeyVault.Commands;

[UsedImplicitly]
[CommandDefinition("keyvault delete", "key-vault", "Deletes a Key Vault.")]
[CommandExample("Delete a Key Vault", "topaz keyvault delete \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"kvlocal\"")]
public sealed class DeleteKeyVaultCommand(Pipeline eventPipeline, ITopazLogger logger) : Command<DeleteKeyVaultCommand.DeleteKeyVaultCommandSettings>
{
    public override int Execute(CommandContext context, DeleteKeyVaultCommandSettings settings)
    {
        logger.LogInformation("Deleting Azure Key Vault...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var controlPlane = new KeyVaultControlPlane(new KeyVaultResourceProvider(logger),
            new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger),
                SubscriptionControlPlane.New(eventPipeline, logger), logger),
            SubscriptionControlPlane.New(eventPipeline, logger), logger);
        
        controlPlane.Delete(subscriptionIdentifier, resourceGroupIdentifier, settings.Name!);

        logger.LogInformation("Azure Key Vault deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteKeyVaultCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Azure Key Vault name can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Key Vault resource group can't be null.");
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
    public sealed class DeleteKeyVaultCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Key Vault name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
        
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
        
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
    }
}
