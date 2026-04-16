using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands.Blob;

[UsedImplicitly]
public sealed class CopyBlobCommand(ITopazLogger logger) : Command<CopyBlobCommand.CopyBlobCommandSettings>
{
    public override int Execute(CommandContext context, CopyBlobCommandSettings settings)
    {
        logger.LogInformation("Copying blob...");

        var srcSubscriptionId = SubscriptionIdentifier.From(settings.SourceSubscriptionId);
        var srcResourceGroupId = ResourceGroupIdentifier.From(settings.SourceResourceGroup);
        var dstSubscriptionId = SubscriptionIdentifier.From(settings.DestinationSubscriptionId);
        var dstResourceGroupId = ResourceGroupIdentifier.From(settings.DestinationResourceGroup);

        var srcBlobPath = $"/{settings.SourceContainerName}/{settings.SourceBlobName}";
        var dstBlobPath = $"/{settings.DestinationContainerName}/{settings.DestinationBlobName}";

        var dataPlane = new BlobServiceDataPlane(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);
        var op = dataPlane.CopyBlob(
            srcSubscriptionId, srcResourceGroupId, settings.SourceAccountName!,
            srcBlobPath,
            dstSubscriptionId, dstResourceGroupId, settings.DestinationAccountName!,
            dstBlobPath, settings.DestinationBlobName!);

        if (op.Result == OperationResult.NotFound)
        {
            logger.LogError($"Source blob '{srcBlobPath}' not found.");
            return 1;
        }

        if (op.Result != OperationResult.Accepted)
        {
            logger.LogError($"Copy failed with status {op.Result}.");
            return 1;
        }

        logger.LogInformation($"Blob copied: {srcBlobPath} -> {dstBlobPath} (copy-id: {op.Resource!.CopyId})");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CopyBlobCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.SourceAccountName))
            return ValidationResult.Error("Source storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.SourceContainerName))
            return ValidationResult.Error("Source container name can't be null.");
        if (string.IsNullOrEmpty(settings.SourceBlobName))
            return ValidationResult.Error("Source blob name can't be null.");
        if (string.IsNullOrEmpty(settings.SourceResourceGroup))
            return ValidationResult.Error("Source resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SourceSubscriptionId))
            return ValidationResult.Error("Source subscription ID can't be null.");
        if (string.IsNullOrEmpty(settings.DestinationAccountName))
            return ValidationResult.Error("Destination storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.DestinationContainerName))
            return ValidationResult.Error("Destination container name can't be null.");
        if (string.IsNullOrEmpty(settings.DestinationBlobName))
            return ValidationResult.Error("Destination blob name can't be null.");
        if (string.IsNullOrEmpty(settings.DestinationResourceGroup))
            return ValidationResult.Error("Destination resource group can't be null.");
        if (string.IsNullOrEmpty(settings.DestinationSubscriptionId))
            return ValidationResult.Error("Destination subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CopyBlobCommandSettings : CommandSettings
    {
        [CommandOption("--source-account-name")] public string? SourceAccountName { get; set; }
        [CommandOption("--source-container")] public string? SourceContainerName { get; set; }
        [CommandOption("--source-blob")] public string? SourceBlobName { get; set; }
        [CommandOption("--source-resource-group")] public string? SourceResourceGroup { get; set; }
        [CommandOption("--source-subscription-id")] public string? SourceSubscriptionId { get; set; }

        [CommandOption("--dest-account-name")] public string? DestinationAccountName { get; set; }
        [CommandOption("--dest-container")] public string? DestinationContainerName { get; set; }
        [CommandOption("--dest-blob")] public string? DestinationBlobName { get; set; }
        [CommandOption("--dest-resource-group")] public string? DestinationResourceGroup { get; set; }
        [CommandOption("--dest-subscription-id")] public string? DestinationSubscriptionId { get; set; }
    }
}
