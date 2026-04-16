using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public sealed class SetContainerMetadataCommand(ITopazLogger logger)
    : Command<SetContainerMetadataCommand.SetContainerMetadataCommandSettings>
{
    public override int Execute(CommandContext context, SetContainerMetadataCommandSettings settings)
    {
        logger.LogInformation("Setting container metadata...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var dataPlane = new BlobServiceDataPlane(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

        var metadata = ParseMetadata(settings.Metadata ?? []);
        var result = dataPlane.SetContainerMetadata(subscriptionIdentifier, resourceGroupIdentifier,
            settings.AccountName!, settings.Name!, metadata);

        if (result.Result == OperationResult.NotFound)
        {
            logger.LogError($"Container '{settings.Name}' not found.");
            return 1;
        }

        logger.LogInformation($"Metadata for container '{settings.Name}' set.");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, SetContainerMetadataCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Container name can't be null.");
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    private static Dictionary<string, string> ParseMetadata(string[] pairs)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in pairs)
        {
            var idx = pair.IndexOf('=');
            if (idx <= 0) continue;
            var key = $"x-ms-meta-{pair[..idx].Trim()}";
            var value = pair[(idx + 1)..].Trim();
            result[key] = value;
        }
        return result;
    }

    [UsedImplicitly]
    public sealed class SetContainerMetadataCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")] public string? Name { get; set; }
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }

        /// <summary>Key=value metadata pairs, e.g. --metadata "env=prod" "owner=team".</summary>
        [CommandOption("--metadata")] public string[]? Metadata { get; set; }
    }
}
