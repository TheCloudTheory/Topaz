using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Chaos.Commands;

[UsedImplicitly]
[CommandDefinition("chaos disable", "chaos", "Disables chaos mode in the Topaz host.")]
[CommandExample("Disable chaos mode", "topaz chaos disable")]
internal sealed class DisableChaosCommand(HttpClient httpClient) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var url = $"https://topaz.local.dev:{Shared.GlobalSettings.DefaultResourceManagerPort}/topaz/chaos/disable";
        var response = await httpClient.PostAsync(url, new StreamContent(Stream.Null));
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
