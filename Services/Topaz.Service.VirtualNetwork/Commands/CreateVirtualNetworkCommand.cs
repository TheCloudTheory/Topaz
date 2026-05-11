using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.VirtualNetwork.Models;
using Topaz.Service.VirtualNetwork.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork.Commands;

[UsedImplicitly]
[CommandDefinition("vnet create", "virtual-network", "Creates or updates an Azure Virtual Network.")]
[CommandExample("Creates a new Virtual Network",
    "topaz vnet create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-vnet\" \\\n    --location \"westeurope\" \\\n    --resource-group \"rg-local\" \\\n    --address-prefix \"10.0.0.0/16\"")]
internal sealed class CreateVirtualNetworkCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<CreateVirtualNetworkCommand.CreateVirtualNetworkCommandSettings>
{
    public override int Execute(CommandContext context, CreateVirtualNetworkCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var controlPlane = VirtualNetworkControlPlane.New(eventPipeline, logger);

        var request = new CreateOrUpdateVirtualNetworkRequest
        {
            Properties = new CreateOrUpdateVirtualNetworkRequest.CreateOrUpdateVirtualNetworkRequestProperties
            {
                AddressSpace = settings.AddressPrefix is not null
                    ? new VirtualNetworkResourceProperties.VirtualNetworkAddressSpace
                    {
                        AddressPrefixes = [settings.AddressPrefix]
                    }
                    : null
            }
        };

        var operation = controlPlane.CreateOrUpdate(
            subscriptionIdentifier, resourceGroupIdentifier, settings.Name!, request);

        if (operation.Result == OperationResult.NotFound)
        {
            Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        AnsiConsole.WriteLine(operation.Resource!.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateVirtualNetworkCommandSettings settings)
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
    public sealed class CreateVirtualNetworkCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) virtual network name")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) location")]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("Address prefix for the virtual network (e.g. 10.0.0.0/16)")]
        [CommandOption("--address-prefix")]
        public string? AddressPrefix { get; set; }
    }
}
