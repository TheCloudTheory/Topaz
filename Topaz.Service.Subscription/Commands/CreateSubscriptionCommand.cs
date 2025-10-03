using JetBrains.Annotations;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Topaz.Service.Subscription.Commands;

[UsedImplicitly]
public sealed class CreateSubscriptionCommand(ITopazLogger logger) : Command<CreateSubscriptionCommand.CreateSubscriptionCommandSettings>
{
    public override int Execute(CommandContext context, CreateSubscriptionCommandSettings settings)
    {
        logger.LogInformation("Creating subscription...");

        var controlPlane = new SubscriptionControlPlane(new SubscriptionResourceProvider(logger));
        var sa = controlPlane.Create(settings.Id, settings.Name!);

        logger.LogInformation(sa.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateSubscriptionCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Id))
        {
            return ValidationResult.Error("Subscription ID can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Subscription name can't be null.");
        }

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateSubscriptionCommandSettings : CommandSettings
    {
        [CommandOption("-i|--id")]
        public string? Id { get; set; }

        [CommandOption("-n|--name")]
        public string? Name { get; set; }
    }
}

