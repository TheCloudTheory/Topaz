using System.Security.Authentication;
using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Shared;

namespace Topaz.CLI.Commands;

[UsedImplicitly]
[CommandDefinition("health", "generic", "Provides information about the health of the host.")]
[CommandExample("Check host health", "topaz health")]
public sealed class HealthCommand(HttpClient httpClient) : AsyncCommand
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}/health", cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
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
        catch (TaskCanceledException)
        {
            AnsiConsole.MarkupLine("[red]Host is not running.[/] Start it with [bold]topaz-host start[/].");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            switch (ex.InnerException)
            {
                case System.Net.Sockets.SocketException { SocketErrorCode: System.Net.Sockets.SocketError.HostNotFound }:
                    AnsiConsole.MarkupLine("[red]DNS lookup failed for [bold]topaz.local.dev[/].[/]");
                    AnsiConsole.MarkupLine("  The domain is not configured locally. See [link]https://topaz.thecloudtheory.com/docs/intro/[/] for setup instructions.");
                    break;
                case AuthenticationException:
                    AnsiConsole.MarkupLine("[red]SSL certificate verification failed for [bold]topaz.local.dev[/].[/]");
                    AnsiConsole.MarkupLine("  The local certificate may not be trusted. See [link]https://topaz.thecloudtheory.com/docs/intro/[/] for setup instructions.");
                    break;
                default:
                    AnsiConsole.MarkupLine("[red]Host is not running.[/] Start it with [bold]topaz-host start[/].");
                    break;
            }

            return 1;
        }
    }
}
