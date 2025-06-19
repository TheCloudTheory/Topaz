using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup.Commands;

[UsedImplicitly]
public sealed class ListResourceGroupCommand(ITopazLogger logger) : Command<ListResourceGroupCommand.ListResourceGroupCommandSettings>
{
    public override int Execute(CommandContext context, ListResourceGroupCommandSettings settings)
    {
        logger.LogDebug($"Executing {nameof(ListResourceGroupCommand)}.{nameof(Execute)}.");

        var controlPlane = new ResourceGroupControlPlane(new ResourceProvider(logger), logger);
        var rg = controlPlane.List(settings.SubscriptionId);

        logger.LogInformation(JsonSerializer.Serialize(rg.resources, GlobalSettings.JsonOptionsCli));

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListResourceGroupCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Resource group subscription ID can't be null.");
        }

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListResourceGroupCommandSettings : CommandSettings
    {
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
    }
}