using JetBrains.Annotations;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.Subscription.Commands;

[UsedImplicitly]
public class DeleteSubscriptionCommand(ITopazLogger logger) : Command<DeleteSubscriptionCommand.DeleteSubscriptionCommandSettings>
{
    public override int Execute(CommandContext context, DeleteSubscriptionCommandSettings settings)
    {
        logger.LogInformation("Deleting subscription...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.Id);
        var controlPlane = new SubscriptionControlPlane(new SubscriptionResourceProvider(logger));
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
        [CommandOption("-i|--id")]
        public string? Id { get; set; }
    }
}
