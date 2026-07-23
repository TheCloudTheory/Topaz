using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualNetwork.Commands;

[UsedImplicitly]
[CommandDefinition("vnet list", "virtual-network", "Lists Azure Virtual Networks in a subscription or resource group.")]
[CommandExample("Lists Virtual Networks in a resource group",
    "topaz vnet list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --resource-group \"rg-local\"")]
internal sealed class ListVirtualNetworksCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<ListVirtualNetworksCommand.ListVirtualNetworksCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, ListVirtualNetworksCommandSettings settings, CancellationToken cancellationToken)
    {
        string url;
        if (!string.IsNullOrWhiteSpace(settings.ResourceGroup))
            url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Network/virtualNetworks";
        else
            url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.Network/virtualNetworks";

        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, ListVirtualNetworksCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        return string.IsNullOrEmpty(settings.SubscriptionId) ? ValidationResult.Error("Subscription ID can't be null.") : base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListVirtualNetworksCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Optional) filter by resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}
