using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage container metadata set", "azure-storage/container", "Sets metadata key-value pairs on a blob container.")]
[CommandExample("Set container metadata", "topaz storage container metadata set \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --name \"mycontainer\" \\\n    --metadata \"env=prod\" \"owner=team\"")]
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
        [CommandOptionDefinition("(Required) Container name.", required: true)]
        [CommandOption("-n|--name")] public string? Name { get; set; }
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("Metadata key=value pairs (e.g. --metadata \"env=prod\" \"owner=team\").")]
        [CommandOption("--metadata")] public string[]? Metadata { get; set; }
    }
}
