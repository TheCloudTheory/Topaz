using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage account check-name", "azure-storage/account", "Checks whether a storage account name is available.")]
[CommandExample("Check name availability", "topaz storage account check-name \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --name \"salocal\"")]
public sealed class CheckStorageAccountNameAvailabilityCommand(ITopazLogger logger)
    : Command<CheckStorageAccountNameAvailabilityCommand.CheckStorageAccountNameAvailabilityCommandSettings>
{
    public override int Execute(CommandContext context, CheckStorageAccountNameAvailabilityCommandSettings settings)
    {
        AnsiConsole.WriteLine("Checking storage account name availability...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var controlPlane = new AzureStorageControlPlane(new StorageResourceProvider(logger), logger);
        var operation = controlPlane.CheckNameAvailability(subscriptionIdentifier, settings.Name!, null);

        if (operation.Result == OperationResult.Failed || operation.Resource == null)
        {
            Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        AnsiConsole.WriteLine(operation.Resource.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CheckStorageAccountNameAvailabilityCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Storage account name can't be null.");

        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CheckStorageAccountNameAvailabilityCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name to check.", required: true)]
        [CommandOption("-n|--name")] public string? Name { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string SubscriptionId { get; set; } = null!;
    }
}
