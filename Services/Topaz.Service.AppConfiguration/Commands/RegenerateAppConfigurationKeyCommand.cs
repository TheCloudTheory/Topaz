using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.AppConfiguration.Commands;

[UsedImplicitly]
[CommandDefinition("appconfig regenerate-key", "app-configuration", "Regenerates an access key for an Azure App Configuration store.")]
[CommandExample("Regenerates the Primary access key",
    "topaz appconfig regenerate-key --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-appconfig\" \\\n    --resource-group \"rg-local\" \\\n    --key-id \"Primary\"")]
internal sealed class RegenerateAppConfigurationKeyCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<RegenerateAppConfigurationKeyCommand.Settings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.AppConfiguration/configurationStores/{settings.Name}/regenerateKey";
        var (success, body) = await PostAsync(url, new { id = settings.KeyId });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Store name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (string.IsNullOrEmpty(settings.KeyId))
            return ValidationResult.Error("Key ID can't be null (e.g. Primary, Secondary, Primary Read Only, Secondary Read Only).");
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

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Required) Key ID to regenerate (Primary, Secondary, Primary Read Only, Secondary Read Only).", required: true)]
        [CommandOption("--key-id")]
        public string? KeyId { get; set; }
    }
}
