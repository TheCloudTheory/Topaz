using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualNetwork.Commands;

[UsedImplicitly]
[CommandDefinition("vnet check-ip", "virtual-network", "Checks whether a private IP address is available for use in a Virtual Network.")]
[CommandExample("Check IP address availability",
    "topaz vnet check-ip --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-vnet\" \\\n    --resource-group \"rg-local\" \\\n    --ip-address \"10.0.1.5\"")]
internal sealed class CheckIpAddressAvailabilityCommand(HttpClient httpClient)
    : TopazHttpCommand<CheckIpAddressAvailabilityCommand.CheckIpAddressAvailabilityCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CheckIpAddressAvailabilityCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Network/virtualNetworks/{settings.Name}/CheckIPAddressAvailability?ipAddress={settings.IpAddress}";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, CheckIpAddressAvailabilityCommandSettings settings)
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
