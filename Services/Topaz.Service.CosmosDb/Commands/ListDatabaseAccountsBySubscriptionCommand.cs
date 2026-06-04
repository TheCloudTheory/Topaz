using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.CosmosDb.Commands;

[UsedImplicitly]
[CommandDefinition("cosmosdb account list-by-subscription", "cosmos-db", "Lists all Azure Cosmos DB accounts in a subscription.")]
[CommandExample("List Cosmos DB accounts in a subscription", "topaz cosmosdb account list-by-subscription \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\"")]
public sealed class ListDatabaseAccountsBySubscriptionCommand(HttpClient httpClient)
    : TopazHttpCommand<ListDatabaseAccountsBySubscriptionCommand.ListDatabaseAccountsBySubscriptionCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, ListDatabaseAccountsBySubscriptionCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.DocumentDB/databaseAccounts";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListDatabaseAccountsBySubscriptionCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListDatabaseAccountsBySubscriptionCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
    }
}
