using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.CosmosDb.Commands;

[UsedImplicitly]
[CommandDefinition("cosmosdb account regenerate-key", "cosmos-db", "Regenerates an access key for an Azure Cosmos DB account.")]
[CommandExample("Regenerate the primary key for a Cosmos DB account", "topaz cosmosdb account regenerate-key \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"my-cosmos-account\" \\\n    --key-kind \"primary\"")]
public sealed class RegenerateKeyDatabaseAccountCommand(HttpClient httpClient)
    : TopazHttpCommand<RegenerateKeyDatabaseAccountCommand.RegenerateKeyDatabaseAccountCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, RegenerateKeyDatabaseAccountCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.DocumentDB/databaseAccounts/{settings.Name}/regenerateKey";
        var body = new { keyKind = settings.KeyKind };
        var (success, _) = await PostAsync(url, body);
        if (!success) return 1;
        AnsiConsole.WriteLine("Key regenerated successfully.");
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, RegenerateKeyDatabaseAccountCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Cosmos DB account name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        var validKeyKinds = new[] { "primary", "secondary", "primaryReadonly", "secondaryReadonly" };
        if (!validKeyKinds.Contains(settings.KeyKind, StringComparer.OrdinalIgnoreCase))
            return ValidationResult.Error($"Key kind must be one of: {string.Join(", ", validKeyKinds)}.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class RegenerateKeyDatabaseAccountCommandSettings : CommandSettings
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

        [CommandOptionDefinition("(Required) Key kind to regenerate: primary, secondary, primaryReadonly, secondaryReadonly.", required: true)]
        [CommandOption("-k|--key-kind")]
        public string KeyKind { get; set; } = null!;
    }
}
