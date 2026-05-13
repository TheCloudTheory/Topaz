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
[CommandDefinition("vnet check-ip", "virtual-network", "Checks whether a private IP address is available for use in a Virtual Network.")]
[CommandExample("Check IP address availability",
    "topaz vnet check-ip --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-vnet\" \\\n    --resource-group \"rg-local\" \\\n    --ip-address \"10.0.1.5\"")]
internal sealed class CheckIpAddressAvailabilityCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<CheckIpAddressAvailabilityCommand.CheckIpAddressAvailabilityCommandSettings>
{
    public override int Execute(CommandContext context, CheckIpAddressAvailabilityCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var controlPlane = VirtualNetworkControlPlane.New(eventPipeline, logger);

        var operation = controlPlane.CheckIpAddressAvailability(
            subscriptionIdentifier, resourceGroupIdentifier, settings.Name!, settings.IpAddress!);

        if (operation.Result == OperationResult.NotFound)
        {
            Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        AnsiConsole.WriteLine(operation.Resource!.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CheckIpAddressAvailabilityCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Virtual network name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (string.IsNullOrEmpty(settings.IpAddress))
            return ValidationResult.Error("IP address can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CheckIpAddressAvailabilityCommandSettings : CommandSettings
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

        [CommandOptionDefinition("(Required) private IP address to check")]
        [CommandOption("-i|--ip-address")]
        public string? IpAddress { get; set; }
    }
}
