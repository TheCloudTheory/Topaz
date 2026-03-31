using JetBrains.Annotations;
using Topaz.Documentation.Command;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.Subscription.Commands;

[UsedImplicitly]
[CommandDefinition("subscription cancel", "subscription", "Cancels a subscription, setting its state to Disabled.")]
[CommandExample("Cancel a subscription", "topaz subscription cancel \\\n    --id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\"")]
public sealed class CancelSubscriptionCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<CancelSubscriptionCommand.CancelSubscriptionCommandSettings>
{
    public override int Execute(CommandContext context, CancelSubscriptionCommandSettings settings)
    {
        logger.LogInformation("Cancelling subscription...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.Id);
        var controlPlane = SubscriptionControlPlane.New(eventPipeline, logger);
        var result = controlPlane.Cancel(subscriptionIdentifier);

        if (result.Result == OperationResult.NotFound)
        {
            logger.LogError($"Subscription '{settings.Id}' not found.");
            return 1;
        }

        logger.LogInformation($"Subscription '{settings.Id}' cancelled successfully.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CancelSubscriptionCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Id))
        {
            return ValidationResult.Error("Subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.Id, out _))
        {
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        }

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CancelSubscriptionCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-i|--id")] public string? Id { get; set; }
    }
}
