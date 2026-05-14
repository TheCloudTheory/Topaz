using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork.Commands;

[UsedImplicitly]
[CommandDefinition("vnet list", "virtual-network", "Lists Azure Virtual Networks in a subscription or resource group.")]
[CommandExample("Lists Virtual Networks in a resource group",
    "topaz vnet list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --resource-group \"rg-local\"")]
internal sealed class ListVirtualNetworksCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<ListVirtualNetworksCommand.ListVirtualNetworksCommandSettings>
{
    public override int Execute(CommandContext context, ListVirtualNetworksCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var controlPlane = VirtualNetworkControlPlane.New(eventPipeline, logger);

        if (!string.IsNullOrWhiteSpace(settings.ResourceGroup))
        {
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
            var operation = controlPlane.ListByResourceGroup(subscriptionIdentifier, resourceGroupIdentifier);
            if (operation.Result != OperationResult.Success)
            {
                Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
                return 1;
            }

            AnsiConsole.WriteLine(JsonSerializer.Serialize(operation.Resource, GlobalSettings.JsonOptionsCli));
        }
        else
        {
            var operation = controlPlane.ListBySubscription(subscriptionIdentifier);
            if (operation.Result != OperationResult.Success)
            {
                Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
                return 1;
            }

            AnsiConsole.WriteLine(JsonSerializer.Serialize(operation.Resource, GlobalSettings.JsonOptionsCli));
        }

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListVirtualNetworksCommandSettings settings)
    {
        return string.IsNullOrEmpty(settings.SubscriptionId) ? ValidationResult.Error("Subscription ID can't be null.") : base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListVirtualNetworksCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Optional) filter by resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}
