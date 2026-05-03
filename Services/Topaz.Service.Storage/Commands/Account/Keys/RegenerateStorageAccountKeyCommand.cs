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
[CommandDefinition("storage account keys renew", "azure-storage/account", "Regenerates an access key for a storage account.")]
[CommandExample("Regenerate key1", "topaz storage account keys renew \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --key-name \"key1\"")]
public sealed class RegenerateStorageAccountKeyCommand(ITopazLogger logger)
    : Command<RegenerateStorageAccountKeyCommand.RegenerateStorageAccountKeyCommandSettings>
{
    public override int Execute(CommandContext context, RegenerateStorageAccountKeyCommandSettings settings)
    {
        AnsiConsole.WriteLine($"Regenerating storage account key '{settings.KeyName}'...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var controlPlane = new AzureStorageControlPlane(new StorageResourceProvider(logger), logger);

        var result = controlPlane.RegenerateKey(subscriptionIdentifier, resourceGroupIdentifier, settings.AccountName!, settings.KeyName!);

        if (result.Result == OperationResult.NotFound)
        {
            Console.Error.WriteLine($"Storage account '{settings.AccountName}' not found.");
            return 1;
        }

        if (result.Result == OperationResult.Failed || result.Resource == null)
        {
            Console.Error.WriteLine("There was an error regenerating the storage account key.");
            return 1;
        }

        var keys = new ListKeysResponse(result.Resource.Keys);
        AnsiConsole.WriteLine(keys.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, RegenerateStorageAccountKeyCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");

        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");

        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        if (string.IsNullOrEmpty(settings.KeyName))
            return ValidationResult.Error("Key name can't be null.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class RegenerateStorageAccountKeyCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("-n|--account-name")] public string? AccountName { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) The key to regenerate (key1 or key2).", required: true)]
        [CommandOption("-k|--key-name")] public string? KeyName { get; set; }
    }
}
