using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Topaz.Service.Subscription.Commands;

public sealed class CreateSubscriptionCommand(ITopazLogger logger) : Command<CreateSubscriptionCommand.CreateSubscriptionCommandSettings>
{
    private readonly ITopazLogger _topazLogger = logger;

    public override int Execute(CommandContext context, CreateSubscriptionCommandSettings settings)
    {
        this._topazLogger.LogInformation("Creating subscription...");

        var controlPlane = new SubscriptionControlPlane(new ResourceProvider(this._topazLogger));
        var sa = controlPlane.Create(settings.Id, settings.Name!);

        this._topazLogger.LogInformation(sa.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateSubscriptionCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Subscription name can't be null.");
        }

        return base.Validate(context, settings);
    }

    public sealed class CreateSubscriptionCommandSettings : CommandSettings
    {
        [CommandOption("-i|--id")]
        public string? Id { get; set; }

        [CommandOption("-n|--name")]
        public string? Name { get; set; }
    }
}

