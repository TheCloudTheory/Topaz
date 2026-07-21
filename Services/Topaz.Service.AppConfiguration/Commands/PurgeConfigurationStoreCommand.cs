using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.AppConfiguration.Commands;

[UsedImplicitly]
[CommandDefinition("appconfig purge", "app-configuration", "Purges an Azure App Configuration store.")]
[CommandExample("Purges an App Configuration store",
    "topaz appconfig purge --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-appconfig\" \\\n    --location \"westeurope\"")]
internal sealed class PurgeConfigurationStoreCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<PurgeConfigurationStoreCommand.Settings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken)
    {
        var url =
            $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.AppConfiguration/locations/{settings.Location}/deletedConfigurationStores/{settings.Name}/purge";
        var (success, body) = await PostAsync(url, new {}, cancellationToken);
        if (!success) return 1;
        AnsiConsole.WriteLine($"App Configuration store '{settings.Name}' deleted.");
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, Settings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.Location ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Store name can't be null.");
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

        [CommandOptionDefinition("(Required) Configuration store location.", required: true)]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}