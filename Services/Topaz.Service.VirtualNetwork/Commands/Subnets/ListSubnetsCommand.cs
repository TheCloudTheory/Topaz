using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.VirtualNetwork.Models;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork.Commands.Subnets;

[UsedImplicitly]
[CommandDefinition("vnet subnet list", "virtual-network", "Lists subnets in a Virtual Network.")]
[CommandExample("Lists all subnets",
    "topaz vnet subnet list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --vnet-name \"my-vnet\" \\\n    --resource-group \"rg-local\"")]
internal sealed class ListSubnetsCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<ListSubnetsCommand.ListSubnetsCommandSettings>
{
    public override int Execute(CommandContext context, ListSubnetsCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var controlPlane = SubnetControlPlane.New(eventPipeline, logger);

        var operation = controlPlane.List(
            subscriptionIdentifier, resourceGroupIdentifier, settings.VnetName!);

        if (operation.Result == OperationResult.NotFound)
        {
            Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        AnsiConsole.WriteLine(JsonSerializer.Serialize(operation.Resource, GlobalSettings.JsonOptionsCli));
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListSubnetsCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.VnetName))
            return ValidationResult.Error("Virtual network name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListSubnetsCommandSettings : CommandSettings
    {
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
