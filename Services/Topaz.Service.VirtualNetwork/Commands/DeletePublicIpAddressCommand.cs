using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualNetwork.Commands;

[UsedImplicitly]
[CommandDefinition("pip delete", "public-ip-address", "Deletes an Azure Public IP Address.")]
[CommandExample("Deletes a Public IP Address",
    "topaz pip delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-pip\" \\\n    --resource-group \"rg-local\"")]
internal sealed class DeletePublicIpAddressCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<DeletePublicIpAddressCommand.DeletePublicIpAddressCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, DeletePublicIpAddressCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Network/publicIPAddresses/{settings.Name}";
        if (!await DeleteAsync(url)) return 1;
        AnsiConsole.WriteLine($"Public IP address '{settings.Name}' deleted.");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeletePublicIpAddressCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Public IP address name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class DeletePublicIpAddressCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) public IP address name")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
