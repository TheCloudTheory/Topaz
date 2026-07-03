using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage table insert-entity", "azure-storage/table", "Inserts an entity into a storage table.")]
[CommandExample("Insert an entity", "topaz storage table insert-entity \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --table-name \"mytable\" \\\n    --entity '{\"PartitionKey\":\"pk1\",\"RowKey\":\"rk1\",\"Value\":\"hello\"}'")]
public sealed class InsertTableEntityCommand(HttpClient httpClient)
    : TopazHttpCommand<InsertTableEntityCommand.InsertTableEntityCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, InsertTableEntityCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{TableDataPlaneUrl(settings.AccountName!)}/{settings.TableName}";
        using var content = new StringContent(settings.EntityJson!, System.Text.Encoding.UTF8, "application/json");
        var response = await HttpClient.PostAsync(url, content);
        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return 1;
        }
        AnsiConsole.WriteLine("Entity inserted.");
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, InsertTableEntityCommandSettings settings)
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
