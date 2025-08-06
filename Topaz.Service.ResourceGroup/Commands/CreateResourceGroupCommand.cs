using JetBrains.Annotations;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ResourceGroup.Commands;

[UsedImplicitly]
public sealed class CreateResourceGroupCommand(ITopazLogger logger) : Command<CreateResourceGroupCommand.CreateResourceGroupCommandSettings>
{
    public override int Execute(CommandContext context, CreateResourceGroupCommandSettings settings)
    {
        logger.LogDebug("Creating a resource group...");

        var controlPlane = new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), logger);
        var rg = controlPlane.Create(ResourceGroupIdentifier.From(settings.Name!), SubscriptionIdentifier.From(settings.SubscriptionId!), settings.Location!);

        logger.LogInformation(rg.resource.ToString());

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

    [UsedImplicitly]
    public sealed class CreateResourceGroupCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
