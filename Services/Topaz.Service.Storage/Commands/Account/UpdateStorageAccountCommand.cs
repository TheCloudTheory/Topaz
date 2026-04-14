using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public sealed class UpdateStorageAccountCommand(ITopazLogger logger)
    : Command<UpdateStorageAccountCommand.UpdateStorageAccountCommandSettings>
{
    public override int Execute(CommandContext context, UpdateStorageAccountCommandSettings settings)
    {
        logger.LogInformation("Updating storage account...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var controlPlane = new AzureStorageControlPlane(new StorageResourceProvider(logger), logger);

        var request = new UpdateStorageAccountRequest
        {
            Tags = settings.Tags?.ToDictionary(t => t.Split('=')[0], t => t.Split('=').Length > 1 ? t.Split('=')[1] : string.Empty)
        };

        var operation = controlPlane.Update(subscriptionIdentifier, resourceGroupIdentifier, settings.Name!, request);

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

    public override ValidationResult Validate(CommandContext context, UpdateStorageAccountCommandSettings settings)
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
    public sealed class UpdateStorageAccountCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")] public string? Name { get; set; }
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOption("-s|--subscription-id")] public string SubscriptionId { get; set; } = null!;

        [CommandOption("--tags")]
        public string[]? Tags { get; set; }
    }
}
