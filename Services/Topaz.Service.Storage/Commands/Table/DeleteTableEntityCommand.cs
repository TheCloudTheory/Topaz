using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public sealed class DeleteTableEntityCommand(ITopazLogger logger)
    : Command<DeleteTableEntityCommand.DeleteTableEntityCommandSettings>
{
    public override int Execute(CommandContext context, DeleteTableEntityCommandSettings settings)
    {
        logger.LogInformation($"Deleting entity (PartitionKey='{settings.PartitionKey}', RowKey='{settings.RowKey}') from table '{settings.TableName}'...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);

        var dataPlane = new TableServiceDataPlane(new TableResourceProvider(logger), logger);
        dataPlane.DeleteEntity(subscriptionIdentifier, resourceGroupIdentifier,
            settings.TableName!, settings.AccountName!, settings.PartitionKey!, settings.RowKey!,
            settings.IfMatch ?? "*");

        logger.LogInformation("Entity deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteTableEntityCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.TableName))
            return ValidationResult.Error("Table name can't be null.");
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.PartitionKey))
            return ValidationResult.Error("Partition key can't be null.");
        if (string.IsNullOrEmpty(settings.RowKey))
            return ValidationResult.Error("Row key can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class DeleteTableEntityCommandSettings : CommandSettings
    {
        [CommandOption("-t|--table-name")] public string? TableName { get; set; }
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOption("-p|--partition-key")] public string? PartitionKey { get; set; }
        [CommandOption("-r|--row-key")] public string? RowKey { get; set; }
        [CommandOption("--if-match")] public string? IfMatch { get; set; }
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
