using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Shared;

namespace Topaz.CLI.Commands;

[UsedImplicitly]
internal sealed class HealthCommand(HttpClient httpClient) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}/health");

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var status = doc.RootElement.TryGetProperty("status", out var statusEl)
                ? statusEl.GetString() ?? "Unknown"
                : "Unknown";

            var workingDir = doc.RootElement.TryGetProperty("workingDirectory", out var wdEl)
                ? wdEl.GetString() ?? "Unknown"
                : "Unknown";

            var hostVersion = doc.RootElement.TryGetProperty("version", out var verEl)
                ? verEl.GetString() ?? "Unknown"
                : "Unknown";

            AnsiConsole.MarkupLine("[green]Host is running[/]");
            AnsiConsole.MarkupLine($"  Status:      [bold]{status}[/]");
            AnsiConsole.MarkupLine($"  Host version: [dim]{Markup.Escape(hostVersion)}[/]");
            AnsiConsole.MarkupLine($"  CLI version:  [dim]{Markup.Escape(ThisAssembly.AssemblyInformationalVersion)}[/]");
            AnsiConsole.MarkupLine($"  Directory:   [dim]{Markup.Escape(workingDir)}[/]");
            AnsiConsole.MarkupLine($"  Port:        [dim]{GlobalSettings.DefaultResourceManagerPort}[/]");

            return 0;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            AnsiConsole.MarkupLine("[red]Host is not running.[/] Start it with [bold]topaz-host start[/].");
            return 1;
        }
    }
}
