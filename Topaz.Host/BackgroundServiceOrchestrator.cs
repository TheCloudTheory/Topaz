using Spectre.Console;
using Topaz.Shared;

namespace Topaz.Host;

internal sealed class BackgroundServiceOrchestrator(ITopazBackgroundService[] services, ITopazLogger logger)
{
    public static ITopazBackgroundService[] Services = [];
    
    public void StartAll(CancellationToken cancellationToken)
    {
        if (services.Length == 0) return;

        Services = services;
        
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Background Service[/]");

        foreach (var service in services)
        {
            table.AddRow(service.Name);
            _ = service.StartAsync(cancellationToken);
            logger.LogDebug(nameof(BackgroundServiceOrchestrator), nameof(StartAll),
                "Background service '{0}' started.", service.Name);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }
}
