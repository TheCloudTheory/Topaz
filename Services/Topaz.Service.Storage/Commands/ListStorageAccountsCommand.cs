using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public sealed class ListStorageAccountsCommand(ITopazLogger logger)
    : Command<ListStorageAccountsCommand.ListStorageAccountsCommandSettings>
{
    public override int Execute(CommandContext context, ListStorageAccountsCommandSettings settings)
    {
        logger.LogInformation("Listing storage accounts...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var controlPlane = new AzureStorageControlPlane(new ResourceProvider(logger), logger);

        if (!string.IsNullOrEmpty(settings.ResourceGroup))
        {
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
            var operation = controlPlane.List(subscriptionIdentifier, resourceGroupIdentifier);

            if (operation.Result != OperationResult.Success)
            {
                logger.LogError($"({operation.Code}) {operation.Reason}");
                return 1;
            }

            if (operation.Resource == null || operation.Resource.Length == 0)
            {
                logger.LogInformation("No storage accounts found in the resource group.");
                return 0;
            }

            foreach (var account in operation.Resource)
                logger.LogInformation(account.ToString());
        }
        else
        {
            var operation = controlPlane.ListBySubscription(subscriptionIdentifier);

            if (operation.Result != OperationResult.Success)
            {
                logger.LogError($"({operation.Code}) {operation.Reason}");
                return 1;
            }

            if (operation.Resource == null || operation.Resource.Length == 0)
            {
                logger.LogInformation("No storage accounts found in the subscription.");
                return 0;
            }

            foreach (var account in operation.Resource)
                logger.LogInformation(account.ToString());
        }

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListStorageAccountsCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListStorageAccountsCommandSettings : CommandSettings
    {
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;

        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}
