using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands.Blob;

[UsedImplicitly]
[CommandDefinition("storage blob list", "azure-storage/blob", "Lists blobs in a container.")]
[CommandExample("List blobs in a container", "topaz storage blob list \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --container-name \"mycontainer\"")]
public sealed class ListBlobsCommand(ITopazLogger logger) : Command<ListBlobsCommand.ListBlobsCommandSettings>
{
    public override int Execute(CommandContext context, ListBlobsCommandSettings settings)
    {
        AnsiConsole.WriteLine("Listing blobs...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);

        var controlPlane = new BlobServiceControlPlane(new BlobResourceProvider(logger));
        var dataPlane = new BlobServiceDataPlane(controlPlane, logger);
        var result = dataPlane.ListBlobs(subscriptionIdentifier, resourceGroupIdentifier, settings.AccountName!,
            settings.ContainerName!);

        var blobs = result.Resource!.GetBlobs();
        if (blobs.Length == 0)
        {
            AnsiConsole.WriteLine("No blobs found.");
            return 0;
        }

        foreach (var blob in blobs)
            AnsiConsole.WriteLine(blob.Name ?? "(unnamed)");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListBlobsCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.ContainerName))
            return ValidationResult.Error("Container name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListBlobsCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Container name.", required: true)]
        [CommandOption("-c|--container-name")] public string? ContainerName { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
