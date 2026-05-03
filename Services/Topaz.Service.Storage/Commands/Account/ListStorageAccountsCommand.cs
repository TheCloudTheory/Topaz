using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage account list", "azure-storage/account", "Lists Azure Storage accounts.")]
[CommandExample("List all accounts in a subscription", "topaz storage account list \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\"")]
[CommandExample("List accounts in a resource group", "topaz storage account list \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\"")]
public sealed class ListStorageAccountsCommand(ITopazLogger logger)
    : Command<ListStorageAccountsCommand.ListStorageAccountsCommandSettings>
{
    public override int Execute(CommandContext context, ListStorageAccountsCommandSettings settings)
    {
        AnsiConsole.WriteLine("Listing storage accounts...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var controlPlane = new AzureStorageControlPlane(new StorageResourceProvider(logger), logger);

        if (!string.IsNullOrEmpty(settings.ResourceGroup))
        {
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
            var operation = controlPlane.List(subscriptionIdentifier, resourceGroupIdentifier);

            if (operation.Result != OperationResult.Success)
            {
                Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
                return 1;
            }

            if (operation.Resource == null || operation.Resource.Length == 0)
            {
                AnsiConsole.WriteLine("No storage accounts found in the resource group.");
                return 0;
            }

            foreach (var account in operation.Resource)
                AnsiConsole.WriteLine(account.ToString());
        }
        else
        {
            var operation = controlPlane.ListBySubscription(subscriptionIdentifier);

            if (operation.Result != OperationResult.Success)
            {
                Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
                return 1;
            }

            if (operation.Resource == null || operation.Resource.Length == 0)
            {
                AnsiConsole.WriteLine("No storage accounts found in the subscription.");
                return 0;
            }

            foreach (var account in operation.Resource)
                AnsiConsole.WriteLine(account.ToString());
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
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("Resource group name (filters to accounts in this group when specified).")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}
