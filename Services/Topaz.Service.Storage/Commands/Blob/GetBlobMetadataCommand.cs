using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands.Blob;

[UsedImplicitly]
public sealed class GetBlobMetadataCommand(ITopazLogger logger) : Command<GetBlobMetadataCommand.GetBlobMetadataCommandSettings>
{
    public override int Execute(CommandContext context, GetBlobMetadataCommandSettings settings)
    {
        logger.LogInformation("Getting blob metadata...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);

        var blobPath = $"/{settings.ContainerName}/{settings.BlobName}";
        var dataPlane = new BlobServiceDataPlane(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);
        var result = dataPlane.GetBlobMetadata(subscriptionIdentifier, resourceGroupIdentifier,
            settings.AccountName!, blobPath);

        if (result.Result == OperationResult.NotFound)
        {
            logger.LogError($"Blob '{blobPath}' not found.");
            return 1;
        }

        var metadata = result.Resource ?? new Dictionary<string, string>();

        if (metadata.Count == 0)
        {
            logger.LogInformation("No metadata set on this blob.");
            return 0;
        }

        foreach (var (key, value) in metadata)
            logger.LogInformation($"{key}: {value}");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, GetBlobMetadataCommandSettings settings)
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
    public sealed class GetBlobMetadataCommandSettings : CommandSettings
    {
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOption("-c|--container-name")] public string? ContainerName { get; set; }
        [CommandOption("-n|--name")] public string? BlobName { get; set; }
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
