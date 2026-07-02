using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.AppConfiguration.Commands.Kv;

[UsedImplicitly]
[CommandDefinition("appconfig kv list-revisions", "app-configuration", "Lists key-value revisions in an App Configuration store.")]
[CommandExample("List revisions",
    "topaz appconfig kv list-revisions --name \"my-appconfig\"")]
internal sealed class ListRevisionsCommand(HttpClient httpClient)
    : TopazHttpCommand<ListRevisionsCommand.Settings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var query = string.Empty;
        if (!string.IsNullOrEmpty(settings.Key)) query += $"?key={Uri.EscapeDataString(settings.Key)}";
        var url = $"{AppConfigDataPlaneUrl(settings.Name!)}/revisions{query}";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
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
    }
}
