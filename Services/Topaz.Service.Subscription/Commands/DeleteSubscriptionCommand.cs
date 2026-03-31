using JetBrains.Annotations;
using Topaz.Documentation.Command;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.EventPipeline;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.Subscription.Commands;

[UsedImplicitly]
[CommandDefinition("subscription delete", "subscription", "Deletes a subscription.")]
[CommandExample("Delete a subscription", "topaz subscription delete \\\n    --id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\"")]
public class DeleteSubscriptionCommand(Pipeline eventPipeline, ITopazLogger logger) : Command<DeleteSubscriptionCommand.DeleteSubscriptionCommandSettings>
{
    public override int Execute(CommandContext context, DeleteSubscriptionCommandSettings settings)
    {
        logger.LogInformation("Deleting subscription...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.Id);
        var controlPlane = SubscriptionControlPlane.New(eventPipeline, logger);
        controlPlane.Delete(subscriptionIdentifier);

        logger.LogInformation("Subscription deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteSubscriptionCommandSettings settings)
    {
        return string.IsNullOrEmpty(settings.Id) ? ValidationResult.Error("Subscription ID can't be null.") : base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class DeleteSubscriptionCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-i|--id")]
        public string? Id { get; set; }
    }
}
