using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Topaz.Service.Subscription.Commands;

public class DeleteSubscriptionCommand(ITopazLogger logger) : Command<DeleteSubscriptionCommand.DeleteSubscriptionCommandSettings>
{
    private readonly ITopazLogger _topazLogger = logger;

    public override int Execute(CommandContext context, DeleteSubscriptionCommandSettings settings)
    {
        this._topazLogger.LogInformation("Deleting subscription...");

        var controlPlane = new SubscriptionControlPlane(new ResourceProvider(this._topazLogger));
        controlPlane.Delete(settings.Id!);

        this._topazLogger.LogInformation("Subscription deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteSubscriptionCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Id))
        {
            return ValidationResult.Error("Subscription ID can't be null.");
        }

        return base.Validate(context, settings);
    }

    public sealed class DeleteSubscriptionCommandSettings : CommandSettings
    {
        [CommandOption("-i|--id")]
        public string? Id { get; set; }
    }
}
