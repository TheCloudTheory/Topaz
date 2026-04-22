using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage account show-connection-string", "azure-storage/account", "Shows the connection string for a storage account.")]
[CommandExample("Show connection string", "topaz storage account show-connection-string \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"salocal\"")]
public sealed class ShowStorageAccountConnectionStringCommand(ITopazLogger logger) : Command<ShowStorageAccountConnectionStringCommand.ShowStorageAccountConnectionStringCommandSettings>
{
    public override int Execute(CommandContext context, ShowStorageAccountConnectionStringCommandSettings settings)
    {
        logger.LogInformation("Listing storage account connection strings...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var controlPlane = new AzureStorageControlPlane(new StorageResourceProvider(logger), logger);
        var operation = controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, settings.Name!);

        if (operation.Result == OperationResult.Failed || operation.Resource == null)
        {
            logger.LogError("Failed to get storage account connection string");
            return 1;
        }
        
        var connectionString = new StorageAccountConnectionString(settings.Name!, operation.Resource.Keys[0].Value);
        
        logger.LogInformation(connectionString.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ShowStorageAccountConnectionStringCommandSettings settings)
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
    public sealed class ShowStorageAccountConnectionStringCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("-n|--name")] public string? Name { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
    }
}