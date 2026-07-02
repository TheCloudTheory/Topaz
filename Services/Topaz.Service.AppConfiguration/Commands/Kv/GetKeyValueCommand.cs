using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.AppConfiguration.Commands.Kv;

[UsedImplicitly]
[CommandDefinition("appconfig kv show", "app-configuration", "Gets a key-value from an App Configuration store.")]
[CommandExample("Get a key-value",
    "topaz appconfig kv show --name \"my-appconfig\" --key \"MyApp:Settings:FontSize\"")]
internal sealed class GetKeyValueCommand(HttpClient httpClient)
    : TopazHttpCommand<GetKeyValueCommand.Settings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var label = string.IsNullOrEmpty(settings.Label) ? string.Empty : $"?label={Uri.EscapeDataString(settings.Label)}";
        var url = $"{AppConfigDataPlaneUrl(settings.Name!)}/kv/{Uri.EscapeDataString(settings.Key!)}{label}";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Store name can't be null.");
        if (string.IsNullOrEmpty(settings.Key))
            return ValidationResult.Error("Key can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class Settings : CommandSettings
    {
        [CommandOptionDefinition("(Required) App Configuration store name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Key.", required: true)]
        [CommandOption("--key")]
        public string? Key { get; set; }

        [CommandOptionDefinition("Label.")]
        [CommandOption("--label")]
        public string? Label { get; set; }
    }
}
