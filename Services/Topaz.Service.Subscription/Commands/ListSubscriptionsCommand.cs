using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Subscription.Commands;

[UsedImplicitly]
[CommandDefinition("subscription list", "subscription", "Lists all subscriptions.")]
[CommandExample("List all subscriptions", "topaz subscription list")]
public sealed class ListSubscriptionsCommand(Pipeline eventPipeline, ITopazLogger logger) : Command
{
    public override int Execute(CommandContext context)
    {
        var controlPlane = SubscriptionControlPlane.New(eventPipeline, logger);
        var operation = controlPlane.List();

        if (operation.Result == OperationResult.Failed)
        {
            AnsiConsole.MarkupLine("[red]Failed to list subscriptions.[/]");
            return 1;
        }

        AnsiConsole.WriteLine(JsonSerializer.Serialize(operation.Resource, GlobalSettings.JsonOptionsCli));

        return 0;
    }
}