using System.Text;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public sealed class InsertTableEntityCommand(ITopazLogger logger)
    : Command<InsertTableEntityCommand.InsertTableEntityCommandSettings>
{
    public override int Execute(CommandContext context, InsertTableEntityCommandSettings settings)
    {
        logger.LogInformation($"Inserting entity into table '{settings.TableName}'...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);

        var json = settings.EntityJson!;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var dataPlane = new TableServiceDataPlane(new TableResourceProvider(logger), logger);
        dataPlane.InsertEntity(stream, subscriptionIdentifier, resourceGroupIdentifier,
            settings.TableName!, settings.AccountName!);

        logger.LogInformation("Entity inserted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, InsertTableEntityCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.TableName))
            return ValidationResult.Error("Table name can't be null.");
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.EntityJson))
            return ValidationResult.Error("Entity JSON can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class InsertTableEntityCommandSettings : CommandSettings
    {
        [CommandOption("-t|--table-name")] public string? TableName { get; set; }
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOption("-e|--entity")] public string? EntityJson { get; set; }
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
