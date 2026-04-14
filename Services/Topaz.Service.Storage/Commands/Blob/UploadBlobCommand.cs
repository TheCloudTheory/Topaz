using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public sealed class UploadBlobCommand(ITopazLogger logger) : Command<UploadBlobCommand.UploadBlobCommandSettings>
{
    public override int Execute(CommandContext context, UploadBlobCommandSettings settings)
    {
        logger.LogInformation("Uploading blob...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);

        using var stream = File.OpenRead(settings.FilePath!);
        var blobPath = $"/{settings.ContainerName}/{settings.BlobName ?? Path.GetFileName(settings.FilePath)}";

        var dataPlane = new BlobServiceDataPlane(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);
        var result = dataPlane.PutBlob(subscriptionIdentifier, resourceGroupIdentifier, settings.AccountName!,
            blobPath, settings.BlobName ?? Path.GetFileName(settings.FilePath)!, stream);

        if (result.code == System.Net.HttpStatusCode.Created)
            logger.LogInformation($"Blob uploaded: {blobPath}");
        else
            logger.LogError($"Upload failed with status {result.code}.");

        return result.code == System.Net.HttpStatusCode.Created ? 0 : 1;
    }

    public override ValidationResult Validate(CommandContext context, UploadBlobCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.ContainerName))
            return ValidationResult.Error("Container name can't be null.");
        if (string.IsNullOrEmpty(settings.FilePath))
            return ValidationResult.Error("File path can't be null.");
        if (!File.Exists(settings.FilePath))
            return ValidationResult.Error($"File '{settings.FilePath}' does not exist.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class UploadBlobCommandSettings : CommandSettings
    {
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOption("-c|--container-name")] public string? ContainerName { get; set; }
        [CommandOption("-f|--file")] public string? FilePath { get; set; }
        [CommandOption("-n|--name")] public string? BlobName { get; set; }
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
