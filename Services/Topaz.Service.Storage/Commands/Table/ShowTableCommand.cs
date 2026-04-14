using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public sealed class ShowTableCommand(ITopazLogger logger) : Command<ShowTableCommand.ShowTableCommandSettings>
{
    public override int Execute(CommandContext context, ShowTableCommandSettings settings)
    {
        logger.LogInformation($"Getting table '{settings.Name}'...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);

        var controlPlane = new TableServiceControlPlane(new TableResourceProvider(logger), logger);
        var exists = controlPlane.CheckIfTableExists(subscriptionIdentifier, resourceGroupIdentifier,
            settings.AccountName!, settings.Name!);

        if (!exists)
        {
            logger.LogError($"Table '{settings.Name}' not found.");
            return 1;
        }

        logger.LogInformation($"Name: {settings.Name}");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ShowTableCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Table name can't be null.");
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ShowTableCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")] public string? Name { get; set; }
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
