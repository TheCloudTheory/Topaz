using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console.Cli;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Subscription.Commands;

[UsedImplicitly]
public sealed class ListSubscriptionsCommand(ITopazLogger logger) : Command
{
    public override int Execute(CommandContext context)
    {
        logger.LogInformation("Listing available subscriptions...");

        var controlPlane = new SubscriptionControlPlane(new ResourceProvider(logger));
        var operation = controlPlane.List();

        if (operation.result == OperationResult.Failed)
        {
            logger.LogError("Failed to list subscriptions.");
            return 1;
        }

        logger.LogInformation(JsonSerializer.Serialize(operation.resource, GlobalSettings.JsonOptionsCli));

        return 0;
    }
}