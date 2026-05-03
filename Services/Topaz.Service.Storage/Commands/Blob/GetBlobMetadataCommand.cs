using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands.Blob;

[UsedImplicitly]
[CommandDefinition("storage blob metadata show", "azure-storage/blob", "Shows the metadata key-value pairs on a blob.")]
[CommandExample("Show blob metadata", "topaz storage blob metadata show \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --container-name \"mycontainer\" \\\n    --name \"file.txt\"")]
public sealed class GetBlobMetadataCommand(ITopazLogger logger) : Command<GetBlobMetadataCommand.GetBlobMetadataCommandSettings>
{
    public override int Execute(CommandContext context, GetBlobMetadataCommandSettings settings)
    {
        AnsiConsole.WriteLine("Getting blob metadata...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);

        var blobPath = $"/{settings.ContainerName}/{settings.BlobName}";
        var dataPlane = new BlobServiceDataPlane(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);
        var result = dataPlane.GetBlobMetadata(subscriptionIdentifier, resourceGroupIdentifier,
            settings.AccountName!, blobPath);

        if (result.Result == OperationResult.NotFound)
        {
            Console.Error.WriteLine($"Blob '{blobPath}' not found.");
            return 1;
        }

        var metadata = result.Resource ?? new Dictionary<string, string>();

        if (metadata.Count == 0)
        {
            AnsiConsole.WriteLine("No metadata set on this blob.");
            return 0;
        }

        foreach (var (key, value) in metadata)
            AnsiConsole.WriteLine($"{key}: {value}");

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
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Container name.", required: true)]
        [CommandOption("-c|--container-name")] public string? ContainerName { get; set; }
        [CommandOptionDefinition("(Required) Blob name.", required: true)]
        [CommandOption("-n|--name")] public string? BlobName { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
