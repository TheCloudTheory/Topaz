using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands.Blob;

[UsedImplicitly]
public sealed class DownloadBlobCommand(ITopazLogger logger) : Command<DownloadBlobCommand.DownloadBlobCommandSettings>
{
    public override int Execute(CommandContext context, DownloadBlobCommandSettings settings)
    {
        logger.LogInformation("Downloading blob...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);

        var blobPath = $"/{settings.ContainerName}/{settings.BlobName}";
        var dataPlane = new BlobServiceDataPlane(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);
        var result = dataPlane.GetBlob(subscriptionIdentifier, resourceGroupIdentifier, settings.AccountName!, blobPath);

        if (result.code == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogError($"Blob '{blobPath}' not found.");
            return 1;
        }

        var destination = settings.Destination ?? settings.BlobName!;
        File.WriteAllText(destination, result.content);
        logger.LogInformation($"Blob downloaded to: {destination}");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DownloadBlobCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.ContainerName))
            return ValidationResult.Error("Container name can't be null.");
        if (string.IsNullOrEmpty(settings.BlobName))
            return ValidationResult.Error("Blob name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class DownloadBlobCommandSettings : CommandSettings
    {
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOption("-c|--container-name")] public string? ContainerName { get; set; }
        [CommandOption("-n|--name")] public string? BlobName { get; set; }
        [CommandOption("-d|--destination")] public string? Destination { get; set; }
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
