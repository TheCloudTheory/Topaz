using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands.Blob;

[UsedImplicitly]
public sealed class ListBlobsCommand(ITopazLogger logger) : Command<ListBlobsCommand.ListBlobsCommandSettings>
{
    public override int Execute(CommandContext context, ListBlobsCommandSettings settings)
    {
        logger.LogInformation("Listing blobs...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);

        var controlPlane = new BlobServiceControlPlane(new BlobResourceProvider(logger));
        var dataPlane = new BlobServiceDataPlane(controlPlane, logger);
        var result = dataPlane.ListBlobs(subscriptionIdentifier, resourceGroupIdentifier, settings.AccountName!,
            settings.ContainerName!);

        var blobs = result.GetBlobs();
        if (blobs.Length == 0)
        {
            logger.LogInformation("No blobs found.");
            return 0;
        }

        foreach (var blob in blobs)
            logger.LogInformation(blob.Name ?? "(unnamed)");

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
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOption("-c|--container-name")] public string? ContainerName { get; set; }
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
