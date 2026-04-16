using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands.Blob;

[UsedImplicitly]
public sealed class SetBlobPropertiesCommand(ITopazLogger logger) : Command<SetBlobPropertiesCommand.SetBlobPropertiesCommandSettings>
{
    public override int Execute(CommandContext context, SetBlobPropertiesCommandSettings settings)
    {
        logger.LogInformation("Setting blob properties...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);

        var blobPath = $"/{settings.ContainerName}/{settings.BlobName}";
        var dataPlane = new BlobServiceDataPlane(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);
        var result = dataPlane.SetBlobProperties(subscriptionIdentifier, resourceGroupIdentifier,
            settings.AccountName!, blobPath,
            settings.ContentType,
            settings.ContentEncoding,
            settings.ContentLanguage,
            settings.CacheControl,
            settings.ContentDisposition);

        if (result.Result == OperationResult.NotFound)
        {
            logger.LogError($"Blob '{blobPath}' not found.");
            return 1;
        }

        logger.LogInformation($"Blob '{blobPath}' properties updated.");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, SetBlobPropertiesCommandSettings settings)
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
    public sealed class SetBlobPropertiesCommandSettings : CommandSettings
    {
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOption("-c|--container-name")] public string? ContainerName { get; set; }
        [CommandOption("-n|--name")] public string? BlobName { get; set; }
        [CommandOption("--content-type")] public string? ContentType { get; set; }
        [CommandOption("--content-encoding")] public string? ContentEncoding { get; set; }
        [CommandOption("--content-language")] public string? ContentLanguage { get; set; }
        [CommandOption("--cache-control")] public string? CacheControl { get; set; }
        [CommandOption("--content-disposition")] public string? ContentDisposition { get; set; }
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
