using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.CosmosDb.Commands;

[UsedImplicitly]
[CommandDefinition("cosmosdb account update", "cosmos-db", "Updates tags on an Azure Cosmos DB account.")]
[CommandExample("Update tags on a Cosmos DB account", "topaz cosmosdb account update \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"my-cosmos-account\" \\\n    --tags \"env=dev team=platform\"")]
public sealed class UpdateDatabaseAccountCommand(HttpClient httpClient)
    : TopazHttpCommand<UpdateDatabaseAccountCommand.UpdateDatabaseAccountCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, UpdateDatabaseAccountCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.DocumentDB/databaseAccounts/{settings.Name}";
        var tags = ParseTags(settings.Tags);
        var body = new { tags };
        var (success, responseBody) = await PatchAsync(url, body);
        if (!success) return 1;
        AnsiConsole.WriteLine(responseBody);
        return 0;
    }

    private static Dictionary<string, string> ParseTags(string? tags)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(tags)) return result;
        foreach (var pair in tags.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = pair.IndexOf('=');
            if (separatorIndex <= 0) continue;
            result[pair[..separatorIndex]] = pair[(separatorIndex + 1)..];
        }
        return result;
    }

    public override ValidationResult Validate(CommandContext context, UpdateDatabaseAccountCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Cosmos DB account name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class UpdateDatabaseAccountCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) Cosmos DB account name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Optional) Space-separated tags in key=value format.", required: false)]
        [CommandOption("--tags")]
        public string? Tags { get; set; }
    }
}
