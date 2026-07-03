using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualNetwork.Commands;

[UsedImplicitly]
[CommandDefinition("pip list", "public-ip-address", "Lists Azure Public IP Addresses in a subscription or resource group.")]
[CommandExample("Lists Public IP Addresses in a resource group",
    "topaz pip list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --resource-group \"rg-local\"")]
internal sealed class ListPublicIpAddressesCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<ListPublicIpAddressesCommand.ListPublicIpAddressesCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, ListPublicIpAddressesCommandSettings settings, CancellationToken cancellationToken)
    {
        string url;
        if (!string.IsNullOrWhiteSpace(settings.ResourceGroup))
            url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Network/publicIPAddresses";
        else
            url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.Network/publicIPAddresses";

        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, ListPublicIpAddressesCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        return string.IsNullOrEmpty(settings.SubscriptionId) ? ValidationResult.Error("Subscription ID can't be null.") : base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListPublicIpAddressesCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Optional) filter by resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}
