using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualNetwork.Commands.PrivateEndpoints;

[UsedImplicitly]
[CommandDefinition("vnet private-endpoint list", "virtual-network", "Lists private endpoints in a resource group.")]
[CommandExample("Lists all private endpoints in a resource group",
    "topaz vnet private-endpoint list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --resource-group \"rg-local\"")]
internal sealed class ListPrivateEndpointsByResourceGroupCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<ListPrivateEndpointsByResourceGroupCommand.ListPrivateEndpointsByResourceGroupCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, ListPrivateEndpointsByResourceGroupCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Network/privateEndpoints";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, ListPrivateEndpointsByResourceGroupCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListPrivateEndpointsByResourceGroupCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
