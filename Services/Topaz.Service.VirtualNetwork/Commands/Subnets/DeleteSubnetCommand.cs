using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualNetwork.Commands.Subnets;

[UsedImplicitly]
[CommandDefinition("vnet subnet delete", "virtual-network", "Deletes a subnet from a Virtual Network.")]
[CommandExample("Deletes a subnet",
    "topaz vnet subnet delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --vnet-name \"my-vnet\" \\\n    --name \"my-subnet\" \\\n    --resource-group \"rg-local\"")]
internal sealed class DeleteSubnetCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<DeleteSubnetCommand.DeleteSubnetCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, DeleteSubnetCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Network/virtualNetworks/{settings.VnetName}/subnets/{settings.Name}";
        if (!await DeleteAsync(url)) return 1;
        AnsiConsole.WriteLine($"Subnet '{settings.Name}' deleted.");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteSubnetCommandSettings settings)
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
    public sealed class DeleteSubnetCommandSettings : CommandSettings
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
