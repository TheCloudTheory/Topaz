using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualNetwork.Commands.Subnets;

[UsedImplicitly]
[CommandDefinition("vnet subnet create", "virtual-network", "Creates or updates a subnet in a Virtual Network.")]
[CommandExample("Creates a subnet",
    "topaz vnet subnet create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --vnet-name \"my-vnet\" \\\n    --name \"my-subnet\" \\\n    --address-prefix \"10.0.1.0/24\" \\\n    --resource-group \"rg-local\"")]
internal sealed class CreateSubnetCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CreateSubnetCommand.CreateSubnetCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CreateSubnetCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Network/virtualNetworks/{settings.VnetName}/subnets/{settings.Name}";
        var (success, body) = await PutAsync(url, new { properties = new { addressPrefix = settings.AddressPrefix } });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, CreateSubnetCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
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
