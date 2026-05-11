using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork.Commands.Subnets;

[UsedImplicitly]
[CommandDefinition("vnet subnet show", "virtual-network", "Gets a subnet from a Virtual Network.")]
[CommandExample("Gets a subnet",
    "topaz vnet subnet show --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --vnet-name \"my-vnet\" \\\n    --name \"my-subnet\" \\\n    --resource-group \"rg-local\"")]
internal sealed class GetSubnetCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<GetSubnetCommand.GetSubnetCommandSettings>
{
    public override int Execute(CommandContext context, GetSubnetCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var controlPlane = SubnetControlPlane.New(eventPipeline, logger);

        var operation = controlPlane.Get(
            subscriptionIdentifier, resourceGroupIdentifier, settings.VnetName!, settings.Name!);

        if (operation.Result == OperationResult.NotFound)
        {
            Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        AnsiConsole.WriteLine(operation.Resource!.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, GetSubnetCommandSettings settings)
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
    public sealed class GetSubnetCommandSettings : CommandSettings
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
    }
}
