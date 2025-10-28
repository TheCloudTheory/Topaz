using JetBrains.Annotations;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.KeyVault.Commands;

[UsedImplicitly]
public sealed class DeleteKeyVaultCommand(ITopazLogger logger) : Command<DeleteKeyVaultCommand.DeleteKeyVaultCommandSettings>
{
    public override int Execute(CommandContext context, DeleteKeyVaultCommandSettings settings)
    {
        logger.LogInformation("Deleting Azure Key Vault...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var rp = new KeyVaultResourceProvider(logger);
        
        rp.Delete(subscriptionIdentifier, resourceGroupIdentifier, settings.Name!);

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
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
        
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
        
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
    }
}
