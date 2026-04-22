using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage table list", "azure-storage/table", "Lists tables in a storage account.")]
[CommandExample("List tables", "topaz storage table list \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\"")]
public sealed class ListTablesCommand(ITopazLogger logger) : Command<ListTablesCommand.ListTablesCommandSettings>
{
    public override int Execute(CommandContext context, ListTablesCommandSettings settings)
    {
        logger.LogInformation("Listing tables...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);

        var controlPlane = new TableServiceControlPlane(new TableResourceProvider(logger), logger);
        var tablesOp = controlPlane.GetTables(subscriptionIdentifier, resourceGroupIdentifier, settings.AccountName!);
        var tables = tablesOp.Resource ?? [];

        if (tables.Length == 0)
        {
            logger.LogInformation("No tables found.");
            return 0;
        }

        foreach (var table in tables)
            logger.LogInformation(table.Name ?? "(unnamed)");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListTablesCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListTablesCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
