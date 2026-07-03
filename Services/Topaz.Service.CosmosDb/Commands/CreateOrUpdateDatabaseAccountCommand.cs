using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.CosmosDb.Commands;

[UsedImplicitly]
[CommandDefinition("cosmosdb account create", "cosmos-db", "Creates or updates an Azure Cosmos DB account.")]
[CommandExample("Create a Cosmos DB account", "topaz cosmosdb account create \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"my-cosmos-account\" \\\n    --location \"westeurope\"")]
public sealed class CreateOrUpdateDatabaseAccountCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CreateOrUpdateDatabaseAccountCommand.CreateOrUpdateDatabaseAccountCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CreateOrUpdateDatabaseAccountCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.DocumentDB/databaseAccounts/{settings.Name}";
        var body = new
        {
            location = settings.Location,
            kind = settings.Kind,
            properties = new
            {
                databaseAccountOfferType = "Standard",
                locations = new[]
                {
                    new { locationName = settings.Location, failoverPriority = 0, isZoneRedundant = false }
                }
            }
        };
        var (success, responseBody) = await PutAsync(url, body);
        if (!success) return 1;
        AnsiConsole.WriteLine(responseBody);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, CreateOrUpdateDatabaseAccountCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        settings.Location ??= defaults.Location;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Cosmos DB account name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.Location))
            return ValidationResult.Error("Location can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateOrUpdateDatabaseAccountCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) Cosmos DB account name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Azure region.", required: true)]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Optional) Account kind: GlobalDocumentDB, MongoDB, Parse. Defaults to GlobalDocumentDB.", required: false)]
        [CommandOption("--kind")]
        public string Kind { get; set; } = "GlobalDocumentDB";
    }
}
