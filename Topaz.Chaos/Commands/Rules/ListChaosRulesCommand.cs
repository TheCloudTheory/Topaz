using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Shared;

namespace Topaz.Chaos.Commands.Rules;

[UsedImplicitly]
[CommandDefinition("chaos rule list", "chaos", "Lists all chaos fault rules.")]
[CommandExample("List all chaos rules", "topaz chaos rule list")]
public sealed class ListChaosRulesCommand(HttpClient httpClient) : AsyncCommand
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var url = $"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}/topaz/chaos/rules";
        var response = await httpClient.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {body}");
            return 1;
        }
        AnsiConsole.WriteLine(body);
        return 0;
    }
}
