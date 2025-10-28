using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup.Commands;

[UsedImplicitly]
public sealed class ShowResourceGroupCommand(ITopazLogger logger) : Command<ShowResourceGroupCommand.ShowResourceGroupCommandSettings>
{
    public override int Execute(CommandContext context, ShowResourceGroupCommandSettings settings)
    {
        logger.LogDebug($"Executing {nameof(ShowResourceGroupCommand)}.{nameof(Execute)}.");

        var controlPlane = new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), logger);
        var operation = controlPlane.Get(new SubscriptionIdentifier(Guid.Parse(settings.SubscriptionId)), new ResourceGroupIdentifier(settings.Name!));

        logger.LogInformation(JsonSerializer.Serialize(operation.Resource, GlobalSettings.JsonOptionsCli));

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ShowResourceGroupCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Resource group subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Resource group subscription ID must be a valid GUID.");
        }

        return string.IsNullOrEmpty(settings.Name)
            ? ValidationResult.Error("Resource group name can't be null.")
            : base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ShowResourceGroupCommandSettings : CommandSettings
    {
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
        
        [CommandOption("-n|--name")]
        public string Name { get; set; } = null!;
    }
}