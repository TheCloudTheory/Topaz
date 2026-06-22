using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualNetwork.Commands;

[UsedImplicitly]
[CommandDefinition("pip create", "public-ip-address", "Creates or updates an Azure Public IP Address.")]
[CommandExample("Creates a new Public IP Address",
    "topaz pip create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-pip\" \\\n    --location \"westeurope\" \\\n    --resource-group \"rg-local\"")]
internal sealed class CreatePublicIpAddressCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CreatePublicIpAddressCommand.CreatePublicIpAddressCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreatePublicIpAddressCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Network/publicIPAddresses/{settings.Name}";
        var (success, body) = await PutAsync(url, new
        {
            location = settings.Location,
            properties = new
            {
                publicIPAllocationMethod = settings.AllocationMethod ?? "Dynamic",
                publicIPAddressVersion = settings.AddressVersion ?? "IPv4"
            }
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreatePublicIpAddressCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        settings.Location ??= defaults.Location;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Public IP address name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreatePublicIpAddressCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) public IP address name")]
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

        [CommandOptionDefinition("IP allocation method (Dynamic or Static, default: Dynamic)")]
        [CommandOption("--allocation-method")]
        public string? AllocationMethod { get; set; }

        [CommandOptionDefinition("IP address version (IPv4 or IPv6, default: IPv4)")]
        [CommandOption("--version")]
        public string? AddressVersion { get; set; }
    }
}
