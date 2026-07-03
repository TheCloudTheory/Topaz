using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.AppConfiguration.Commands.Kv;

[UsedImplicitly]
[CommandDefinition("appconfig kv list", "app-configuration", "Lists key-values in an App Configuration store.")]
[CommandExample("List all key-values",
    "topaz appconfig kv list --name \"my-appconfig\"")]
internal sealed class ListKeyValuesCommand(HttpClient httpClient)
    : TopazHttpCommand<ListKeyValuesCommand.Settings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var query = string.Empty;
        if (!string.IsNullOrEmpty(settings.Key)) query += $"?key={Uri.EscapeDataString(settings.Key)}";
        if (!string.IsNullOrEmpty(settings.Label)) query += (query.Length == 0 ? "?" : "&") + $"label={Uri.EscapeDataString(settings.Label)}";
        var url = $"{AppConfigDataPlaneUrl(settings.Name!)}/kv{query}";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Store name can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class Settings : CommandSettings
    {
        [CommandOptionDefinition("(Required) App Configuration store name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("Key filter (supports * wildcard).")]
        [CommandOption("--key")]
        public string? Key { get; set; }

        [CommandOptionDefinition("Label filter.")]
        [CommandOption("--label")]
        public string? Label { get; set; }
    }
}
