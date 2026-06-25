using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Chaos.Commands;

[UsedImplicitly]
[CommandDefinition("chaos enable", "chaos", "Enables chaos mode in the Topaz host.")]
[CommandExample("Enable chaos mode", "topaz chaos enable")]
internal sealed class EnableChaosCommand(HttpClient httpClient) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var url = $"https://topaz.local.dev:{Shared.GlobalSettings.DefaultResourceManagerPort}/topaz/chaos/enable";
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
