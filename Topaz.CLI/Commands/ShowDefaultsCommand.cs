using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;

namespace Topaz.CLI.Commands;

[UsedImplicitly]
internal sealed class ShowDefaultsCommand(DefaultsProvider provider) : AsyncCommand
{
    public override Task<int> ExecuteAsync(CommandContext context)
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