using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Topaz.Service.ResourceGroup.Commands;

public sealed class CreateResourceGroupCommand(ILogger logger) : Command<CreateResourceGroupCommand.CreateResourceGroupCommandSettings>
{
    private readonly ILogger logger = logger;

    public override int Execute(CommandContext context, CreateResourceGroupCommandSettings settings)
    {
        this.logger.LogInformation($"Executing {nameof(CreateResourceGroupCommand)}.{nameof(Execute)}.");

        var controlPlane = new ResourceGroupControlPlane(new ResourceProvider(this.logger));
        var rg = controlPlane.Create(settings.Name!, settings.SubscriptionId!, settings.Location!);

        this.logger.LogInformation(rg.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateResourceGroupCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Resource group name can't be null.");
        }

        if(string.IsNullOrEmpty(settings.Location))
        {
            return ValidationResult.Error("Resource group location can't be null.");
        }

        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Resource group subscription ID can't be null.");
        }

        return base.Validate(context, settings);
    }

    public sealed class CreateResourceGroupCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOption("-s|--subscriptionId")]
        public string? SubscriptionId { get; set; }
    }
}
