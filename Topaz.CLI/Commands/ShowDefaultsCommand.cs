using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.CLI.Commands;

[UsedImplicitly]
[CommandDefinition("configure show", "generic", "Shows default values for the CLI.")]
[CommandExample("Show current CLI defaults", "topaz configure show")]
internal sealed class ShowDefaultsCommand(DefaultsProvider provider) : AsyncCommand
{
    protected override Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var defaults = provider.LoadDefaults();
        
        var subscriptionId = defaults.SubscriptionId ?? "Not set";
        var resourceGroup = defaults.ResourceGroup ?? "Not set";
        var location = defaults.Location ?? "Not set";
        
        AnsiConsole.MarkupLine("Topaz CLI defaults:");
        AnsiConsole.MarkupLine($"  Subscription:      [bold]{subscriptionId}[/]");
        AnsiConsole.MarkupLine($"  Resource group: [bold]{resourceGroup}[/]");
        AnsiConsole.MarkupLine($"  Location:  [bold]{location}[/]");
        
        return Task.FromResult(0);
    }
}