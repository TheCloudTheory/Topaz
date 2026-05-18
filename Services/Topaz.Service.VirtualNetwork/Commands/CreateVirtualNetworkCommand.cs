using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualNetwork.Commands;

[UsedImplicitly]
[CommandDefinition("vnet create", "virtual-network", "Creates or updates an Azure Virtual Network.")]
[CommandExample("Creates a new Virtual Network",
    "topaz vnet create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-vnet\" \\\n    --location \"westeurope\" \\\n    --resource-group \"rg-local\" \\\n    --address-prefix \"10.0.0.0/16\"")]
internal sealed class CreateVirtualNetworkCommand(HttpClient httpClient)
    : TopazHttpCommand<CreateVirtualNetworkCommand.CreateVirtualNetworkCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreateVirtualNetworkCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Network/virtualNetworks/{settings.Name}";
        var (success, body) = await PutAsync(url, new
        {
            location = settings.Location,
            properties = new
            {
                addressSpace = new
                {
                    addressPrefixes = new[] { settings.AddressPrefix ?? "10.0.0.0/16" }
                }
            }
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
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
