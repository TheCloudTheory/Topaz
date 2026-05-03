using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage container list", "azure-storage/container", "Lists blob containers in a storage account.")]
[CommandExample("List containers", "topaz storage container list \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\"")]
public sealed class ListBlobContainersCommand(ITopazLogger logger)
    : Command<ListBlobContainersCommand.ListBlobContainersCommandSettings>
{
    public override int Execute(CommandContext context, ListBlobContainersCommandSettings settings)
    {
        AnsiConsole.WriteLine("Listing blob containers...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var controlPlane = new BlobServiceControlPlane(new BlobResourceProvider(logger));
        var result = controlPlane.ListContainers(subscriptionIdentifier, resourceGroupIdentifier, settings.AccountName!);

        if (result.Result != OperationResult.Success || result.Resource == null)
            return 1;

        var containers = result.Resource.GetContainers();
        if (containers.Length == 0)
        {
            AnsiConsole.WriteLine("No containers found.");
            return 0;
        }

        foreach (var container in containers)
            AnsiConsole.WriteLine(container.Name ?? "(unnamed)");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListBlobContainersCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.AccountName))
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
    public sealed class ListBlobContainersCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("-n|--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string SubscriptionId { get; set; } = null!;
    }
}
