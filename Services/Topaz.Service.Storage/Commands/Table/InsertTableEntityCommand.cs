using System.Text;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage table insert-entity", "azure-storage/table", "Inserts an entity into a storage table.")]
[CommandExample("Insert an entity", "topaz storage table insert-entity \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --table-name \"mytable\" \\\n    --entity '{\"PartitionKey\":\"pk1\",\"RowKey\":\"rk1\",\"Value\":\"hello\"}'")]
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
        [CommandOptionDefinition("(Required) Table name.", required: true)]
        [CommandOption("-t|--table-name")] public string? TableName { get; set; }
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) JSON-encoded entity object with PartitionKey and RowKey.", required: true)]
        [CommandOption("-e|--entity")] public string? EntityJson { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
