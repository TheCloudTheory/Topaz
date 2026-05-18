using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage table query-entity", "azure-storage/table", "Queries entities in a storage table with an optional OData filter.")]
[CommandExample("Query all entities", "topaz storage table query-entity \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --table-name \"mytable\"")]
[CommandExample("Query with filter", "topaz storage table query-entity \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --table-name \"mytable\" \\\n    --filter \"PartitionKey eq 'pk1'\"")]
public sealed class QueryTableEntitiesCommand(HttpClient httpClient)
    : TopazHttpCommand<QueryTableEntitiesCommand.QueryTableEntitiesCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, QueryTableEntitiesCommandSettings settings)
    {
        var qs = new List<string>();
        if (!string.IsNullOrEmpty(settings.Filter)) qs.Add($"$filter={Uri.EscapeDataString(settings.Filter)}");
        var url = $"{TableDataPlaneUrl(settings.AccountName!)}/{settings.TableName}" + (qs.Count > 0 ? "?" + string.Join("&", qs) : string.Empty);
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
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
        [CommandOptionDefinition("(Required) Table name.", required: true)]
        [CommandOption("-t|--table-name")] public string? TableName { get; set; }
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("OData filter expression (e.g. \"PartitionKey eq 'pk1'\").")]
        [CommandOption("-f|--filter")] public string? Filter { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
