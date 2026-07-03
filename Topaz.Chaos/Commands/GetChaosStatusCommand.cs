using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Chaos.Commands;

[UsedImplicitly]
[CommandDefinition("chaos status", "chaos", "Returns the current chaos mode status from the Topaz host.")]
[CommandExample("Get chaos status", "topaz chaos status")]
internal sealed class GetChaosStatusCommand(HttpClient httpClient) : AsyncCommand
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var url = $"https://topaz.local.dev:{Topaz.Shared.GlobalSettings.DefaultResourceManagerPort}/topaz/chaos/status";
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
