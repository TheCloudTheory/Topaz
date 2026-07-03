using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.AppConfiguration.Commands;

[UsedImplicitly]
[CommandDefinition("appconfig create", "app-configuration", "Creates or updates an Azure App Configuration store.")]
[CommandExample("Creates a new App Configuration store",
    "topaz appconfig create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-appconfig\" \\\n    --location \"westeurope\" \\\n    --resource-group \"rg-local\"")]
internal sealed class CreateAppConfigurationStoreCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CreateAppConfigurationStoreCommand.Settings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.AppConfiguration/configurationStores/{settings.Name}";
        var (success, body) = await PutAsync(url, new
        {
            location = settings.Location,
            sku = settings.Sku == null ? null : new { name = settings.Sku },
            properties = new { publicNetworkAccess = "Enabled" }
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, Settings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        settings.Location ??= defaults.Location;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Store name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.Location))
            return ValidationResult.Error("Location can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class Settings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Store name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Location.", required: true)]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Optional) SKU name (e.g. Free, Standard).", required: false)]
        [CommandOption("--sku")]
        public string? Sku { get; set; }
    }
}
