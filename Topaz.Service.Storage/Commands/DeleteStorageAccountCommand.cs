using JetBrains.Annotations;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public class DeleteStorageAccountCommand(ITopazLogger logger) : Command<DeleteStorageAccountCommand.DeleteStorageAccountCommandSettings>
{
    public override int Execute(CommandContext context, DeleteStorageAccountCommandSettings settings)
    {
        logger.LogInformation("Deleting storage account...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var rp = new AzureStorageControlPlane(new ResourceProvider(logger), logger);
        
        rp.Delete(subscriptionIdentifier, resourceGroupIdentifier, settings.Name!);

        logger.LogInformation("Storage account deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteStorageAccountCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Storage account name can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Storage account resource group can't be null.");
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
    public sealed class DeleteStorageAccountCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
        
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;

        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}
