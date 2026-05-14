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
[CommandDefinition("vnet delete", "virtual-network", "Deletes an Azure Virtual Network.")]
[CommandExample("Deletes a Virtual Network",
    "topaz vnet delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-vnet\" \\\n    --resource-group \"rg-local\"")]
internal sealed class DeleteVirtualNetworkCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<DeleteVirtualNetworkCommand.DeleteVirtualNetworkCommandSettings>
{
    public override int Execute(CommandContext context, DeleteVirtualNetworkCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var controlPlane = VirtualNetworkControlPlane.New(eventPipeline, logger);

        var existing = controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, settings.Name!);
        if (existing.Result == OperationResult.NotFound)
        {
            Console.Error.WriteLine($"({existing.Code}) {existing.Reason}");
            return 1;
        }

        controlPlane.Delete(subscriptionIdentifier, resourceGroupIdentifier, settings.Name!);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteVirtualNetworkCommandSettings settings)
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
    public sealed class DeleteVirtualNetworkCommandSettings : CommandSettings
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
