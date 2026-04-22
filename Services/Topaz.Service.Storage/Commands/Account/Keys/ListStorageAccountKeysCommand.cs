using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models.Responses;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage account keys list", "azure-storage/account", "Lists the access keys for a storage account.")]
[CommandExample("List storage account keys", "topaz storage account keys list \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\"")]
public sealed class ListStorageAccountKeysCommand(ITopazLogger logger) : Command<ListStorageAccountKeysCommand.ListStorageAccountKeysCommandSettings>
{
    public override int Execute(CommandContext context, ListStorageAccountKeysCommandSettings settings)
    {
        logger.LogInformation("Fetching storage account keys...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var controlPlane = new AzureStorageControlPlane(new StorageResourceProvider(logger), logger);
        var storageAccount = controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, settings.Name!);

        if (storageAccount.Result == OperationResult.Failed || storageAccount.Result == OperationResult.Failed ||
            storageAccount.Resource == null)
        {
            logger.LogError($"[{storageAccount.Result}] There was an error fetching storage account keys.");
            return 1;
        }

        var keys = new ListKeysResponse(storageAccount.Resource.Keys);
        logger.LogInformation(keys.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListStorageAccountKeysCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Storage account name can't be null.");
        }

        if(string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Service Bus namespace resource group can't be null.");
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
    public sealed class ListStorageAccountKeysCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("-n|--account-name")] public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
    }
}