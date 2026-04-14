using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public sealed class ShowStorageAccountCommand(ITopazLogger logger)
    : Command<ShowStorageAccountCommand.ShowStorageAccountCommandSettings>
{
    public override int Execute(CommandContext context, ShowStorageAccountCommandSettings settings)
    {
        logger.LogInformation("Fetching storage account...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var controlPlane = new AzureStorageControlPlane(new StorageResourceProvider(logger), logger);
        var operation = controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, settings.Name!);

        if (operation.Result == OperationResult.NotFound)
        {
            logger.LogError($"Storage account '{settings.Name}' not found.");
            return 1;
        }

        if (operation.Result == OperationResult.Failed || operation.Resource == null)
        {
            logger.LogError($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        logger.LogInformation(operation.Resource.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ShowStorageAccountCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Storage account name can't be null.");

        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");

        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ShowStorageAccountCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")] public string? Name { get; set; }
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOption("-s|--subscription-id")] public string SubscriptionId { get; set; } = null!;
    }
}
