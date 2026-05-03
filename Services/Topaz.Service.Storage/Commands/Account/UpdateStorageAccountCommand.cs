using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage account update", "azure-storage/account", "Updates an Azure Storage account.")]
[CommandExample("Update tags on a storage account", "topaz storage account update \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"salocal\" \\\n    --tags \"env=prod\" \"owner=team\"")]
public sealed class UpdateStorageAccountCommand(ITopazLogger logger)
    : Command<UpdateStorageAccountCommand.UpdateStorageAccountCommandSettings>
{
    public override int Execute(CommandContext context, UpdateStorageAccountCommandSettings settings)
    {
        AnsiConsole.WriteLine("Updating storage account...");

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
            Console.Error.WriteLine($"Storage account '{settings.Name}' not found.");
            return 1;
        }

        if (operation.Result == OperationResult.Failed || operation.Resource == null)
        {
            Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        AnsiConsole.WriteLine(operation.Resource.ToString());
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
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("-n|--name")] public string? Name { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("Resource tags as key=value pairs.")]
        [CommandOption("--tags")]
        public string[]? Tags { get; set; }
    }
}
