using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.EventPipeline;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup.Commands;

[UsedImplicitly]
public sealed class ListResourceGroupCommand(Pipeline eventPipeline, ITopazLogger logger) : Command<ListResourceGroupCommand.ListResourceGroupCommandSettings>
{
    public override int Execute(CommandContext context, ListResourceGroupCommandSettings settings)
    {
        logger.LogDebug(nameof(ListResourceGroupCommand), nameof(Execute), "Executing {0}.{1}.", nameof(ListResourceGroupCommand), nameof(Execute));

        var controlPlane = new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), new SubscriptionControlPlane(eventPipeline, new SubscriptionResourceProvider(logger)), logger);
        var operation = controlPlane.List(SubscriptionIdentifier.From(settings.SubscriptionId));

        logger.LogInformation(JsonSerializer.Serialize(operation.resources, GlobalSettings.JsonOptionsCli));

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