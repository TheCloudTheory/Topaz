using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models.Responses;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public sealed class ListStorageAccountKeysCommand(ITopazLogger logger) : Command<ListStorageAccountKeysCommand.ListStorageAccountKeysCommandSettings>
{
    public override int Execute(CommandContext context, ListStorageAccountKeysCommandSettings settings)
    {
        logger.LogInformation("Fetching storage account keys...");

        var controlPlane = new AzureStorageControlPlane(new ResourceProvider(logger), logger);
        var storageAccount = controlPlane.Get(settings.Name!);

        if (storageAccount.result == OperationResult.Failed || storageAccount.result == OperationResult.Failed ||
            storageAccount.resource == null)
        {
            logger.LogError($"[{storageAccount.result}] There was an error fetching storage account keys.");
            return 1;
        }

        var keys = new ListKeysResponse(storageAccount.resource.Keys);
        logger.LogInformation(keys.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListStorageAccountKeysCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Storage account name can't be null.");
        }

        if (string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Resource group can't be null.");
        }

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListStorageAccountKeysCommandSettings : CommandSettings
    {
        [CommandOption("-n|--account-name")] public string? Name { get; set; }

        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
    }
}