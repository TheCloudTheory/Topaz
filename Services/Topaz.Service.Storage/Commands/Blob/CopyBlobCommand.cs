using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands.Blob;

[UsedImplicitly]
[CommandDefinition("storage blob copy", "azure-storage/blob", "Copies a blob to a destination container, optionally across accounts.")]
[CommandExample("Copy blob within same account", "topaz storage blob copy \\\n    --source-subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --source-resource-group \"rg-local\" \\\n    --source-account-name \"salocal\" \\\n    --source-container \"src\" \\\n    --source-blob \"file.txt\" \\\n    --dest-subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --dest-resource-group \"rg-local\" \\\n    --dest-account-name \"salocal\" \\\n    --dest-container \"dst\" \\\n    --dest-blob \"file-copy.txt\"")]
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
        [CommandOptionDefinition("(Required) Source storage account name.", required: true)]
        [CommandOption("--source-account-name")] public string? SourceAccountName { get; set; }
        [CommandOptionDefinition("(Required) Source container name.", required: true)]
        [CommandOption("--source-container")] public string? SourceContainerName { get; set; }
        [CommandOptionDefinition("(Required) Source blob name.", required: true)]
        [CommandOption("--source-blob")] public string? SourceBlobName { get; set; }
        [CommandOptionDefinition("(Required) Source resource group name.", required: true)]
        [CommandOption("--source-resource-group")] public string? SourceResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Source subscription ID.", required: true)]
        [CommandOption("--source-subscription-id")] public string? SourceSubscriptionId { get; set; }

        [CommandOptionDefinition("(Required) Destination storage account name.", required: true)]
        [CommandOption("--dest-account-name")] public string? DestinationAccountName { get; set; }
        [CommandOptionDefinition("(Required) Destination container name.", required: true)]
        [CommandOption("--dest-container")] public string? DestinationContainerName { get; set; }
        [CommandOptionDefinition("(Required) Destination blob name.", required: true)]
        [CommandOption("--dest-blob")] public string? DestinationBlobName { get; set; }
        [CommandOptionDefinition("(Required) Destination resource group name.", required: true)]
        [CommandOption("--dest-resource-group")] public string? DestinationResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Destination subscription ID.", required: true)]
        [CommandOption("--dest-subscription-id")] public string? DestinationSubscriptionId { get; set; }
    }
}
