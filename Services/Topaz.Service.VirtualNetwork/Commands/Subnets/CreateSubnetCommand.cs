using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.VirtualNetwork.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork.Commands.Subnets;

[UsedImplicitly]
[CommandDefinition("vnet subnet create", "virtual-network", "Creates or updates a subnet in a Virtual Network.")]
[CommandExample("Creates a subnet",
    "topaz vnet subnet create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --vnet-name \"my-vnet\" \\\n    --name \"my-subnet\" \\\n    --address-prefix \"10.0.1.0/24\" \\\n    --resource-group \"rg-local\"")]
internal sealed class CreateSubnetCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<CreateSubnetCommand.CreateSubnetCommandSettings>
{
    public override int Execute(CommandContext context, CreateSubnetCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var controlPlane = SubnetControlPlane.New(eventPipeline, logger);

        var request = new CreateOrUpdateSubnetRequest
        {
            Properties = new CreateOrUpdateSubnetRequest.CreateOrUpdateSubnetRequestProperties
            {
                AddressPrefix = settings.AddressPrefix,
                AddressPrefixes = settings.AddressPrefix is not null ? [settings.AddressPrefix] : null
            }
        };

        var operation = controlPlane.CreateOrUpdate(
            subscriptionIdentifier, resourceGroupIdentifier, settings.VnetName!, settings.Name!, request);

        if (operation.Result == OperationResult.NotFound)
        {
            Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        AnsiConsole.WriteLine(operation.Resource!.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateSubnetCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Subnet name can't be null.");
        if (string.IsNullOrEmpty(settings.VnetName))
            return ValidationResult.Error("Virtual network name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateSubnetCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) subnet name")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) virtual network name")]
        [CommandOption("--vnet-name")]
        public string? VnetName { get; set; }

        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("Address prefix for the subnet (e.g. 10.0.1.0/24)")]
        [CommandOption("--address-prefix")]
        public string? AddressPrefix { get; set; }
    }
}
