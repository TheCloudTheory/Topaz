using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup.Commands;

[UsedImplicitly]
[CommandDefinition("group list", "group", "Lists all resource groups in a subscription.")]
[CommandExample("List resource groups", "topaz group list \\\n    --subscription-id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\"")]
public sealed class ListResourceGroupCommand(Pipeline eventPipeline, ITopazLogger logger) : Command<ListResourceGroupCommand.ListResourceGroupCommandSettings>
{
    public override int Execute(CommandContext context, ListResourceGroupCommandSettings settings)
    {
        logger.LogDebug(nameof(ListResourceGroupCommand), nameof(Execute), "Executing {0}.{1}.", nameof(ListResourceGroupCommand), nameof(Execute));

        var controlPlane = new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);
        var operation = controlPlane.List(SubscriptionIdentifier.From(settings.SubscriptionId));

        AnsiConsole.WriteLine(JsonSerializer.Serialize(operation.resources, GlobalSettings.JsonOptionsCli));

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
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
    }
}