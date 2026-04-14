using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public sealed class ListBlobContainersCommand(ITopazLogger logger)
    : Command<ListBlobContainersCommand.ListBlobContainersCommandSettings>
{
    public override int Execute(CommandContext context, ListBlobContainersCommandSettings settings)
    {
        logger.LogInformation("Listing blob containers...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var controlPlane = new BlobServiceControlPlane(new BlobResourceProvider(logger));
        var result = controlPlane.ListContainers(subscriptionIdentifier, resourceGroupIdentifier, settings.AccountName!);

        var containers = result.GetContainers();
        if (containers.Length == 0)
        {
            logger.LogInformation("No containers found.");
            return 0;
        }

        foreach (var container in containers)
            logger.LogInformation(container.Name ?? "(unnamed)");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListBlobContainersCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");

        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");

        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListBlobContainersCommandSettings : CommandSettings
    {
        [CommandOption("-n|--account-name")] public string? AccountName { get; set; }
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOption("-s|--subscription-id")] public string SubscriptionId { get; set; } = null!;
    }
}
