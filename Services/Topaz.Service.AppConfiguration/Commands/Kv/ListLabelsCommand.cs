using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.AppConfiguration.Commands.Kv;

[UsedImplicitly]
[CommandDefinition("appconfig kv list-labels", "app-configuration", "Lists all labels in an App Configuration store.")]
[CommandExample("List labels",
    "topaz appconfig kv list-labels --name \"my-appconfig\"")]
internal sealed class ListLabelsCommand(HttpClient httpClient)
    : TopazHttpCommand<ListLabelsCommand.Settings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var url = $"{AppConfigDataPlaneUrl(settings.Name!)}/labels";
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
    }
}
