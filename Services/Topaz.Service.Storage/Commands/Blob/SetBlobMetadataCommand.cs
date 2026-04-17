using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands.Blob;

[UsedImplicitly]
public sealed class SetBlobMetadataCommand(ITopazLogger logger) : Command<SetBlobMetadataCommand.SetBlobMetadataCommandSettings>
{
    public override int Execute(CommandContext context, SetBlobMetadataCommandSettings settings)
    {
        logger.LogInformation("Setting blob metadata...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);

        var blobPath = $"/{settings.ContainerName}/{settings.BlobName}";
        var dataPlane = new BlobServiceDataPlane(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

        var metadata = (settings.Metadata ?? [])
            .Select(m => m.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1]);

        var result = dataPlane.SetBlobMetadata(subscriptionIdentifier, resourceGroupIdentifier,
            settings.AccountName!, blobPath, metadata);

        if (result.Result == OperationResult.NotFound)
        {
            logger.LogError($"Blob '{blobPath}' not found.");
            return 1;
        }

        logger.LogInformation($"Blob '{blobPath}' metadata updated.");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, SetBlobMetadataCommandSettings settings)
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
    public sealed class SetBlobMetadataCommandSettings : CommandSettings
    {
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOption("-c|--container-name")] public string? ContainerName { get; set; }
        [CommandOption("-n|--name")] public string? BlobName { get; set; }
        [CommandOption("--metadata")] public string[]? Metadata { get; set; }
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
