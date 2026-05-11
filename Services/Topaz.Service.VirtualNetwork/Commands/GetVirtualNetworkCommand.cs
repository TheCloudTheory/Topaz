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
[CommandDefinition("vnet show", "virtual-network", "Gets an Azure Virtual Network.")]
[CommandExample("Gets a Virtual Network",
    "topaz vnet show --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-vnet\" \\\n    --resource-group \"rg-local\"")]
internal sealed class GetVirtualNetworkCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<GetVirtualNetworkCommand.GetVirtualNetworkCommandSettings>
{
    public override int Execute(CommandContext context, GetVirtualNetworkCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var controlPlane = VirtualNetworkControlPlane.New(eventPipeline, logger);

        var operation = controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, settings.Name!);

        if (operation.Result == OperationResult.NotFound)
        {
            Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        AnsiConsole.WriteLine(operation.Resource!.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, GetVirtualNetworkCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Virtual network name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class GetVirtualNetworkCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) virtual network name")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
