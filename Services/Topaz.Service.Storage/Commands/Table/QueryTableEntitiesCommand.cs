using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public sealed class QueryTableEntitiesCommand(ITopazLogger logger)
    : Command<QueryTableEntitiesCommand.QueryTableEntitiesCommandSettings>
{
    public override int Execute(CommandContext context, QueryTableEntitiesCommandSettings settings)
    {
        logger.LogInformation($"Querying entities in table '{settings.TableName}'...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);

        var queryString = string.IsNullOrEmpty(settings.Filter)
            ? QueryString.Empty
            : QueryString.Create("$filter", settings.Filter);

        var dataPlane = new TableServiceDataPlane(new TableResourceProvider(logger), logger);
        var entities = dataPlane.QueryEntities(queryString, subscriptionIdentifier, resourceGroupIdentifier,
            settings.TableName!, settings.AccountName!);

        if (entities.Length == 0)
        {
            logger.LogInformation("No entities found.");
            return 0;
        }

        foreach (var entity in entities)
            logger.LogInformation(entity?.ToString() ?? "(null)");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, QueryTableEntitiesCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.TableName))
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
    public sealed class QueryTableEntitiesCommandSettings : CommandSettings
    {
        [CommandOption("-t|--table-name")] public string? TableName { get; set; }
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOption("-f|--filter")] public string? Filter { get; set; }
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
