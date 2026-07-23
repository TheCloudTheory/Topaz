using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualNetwork.Commands.PrivateEndpoints;

[UsedImplicitly]
[CommandDefinition("vnet private-endpoint list-by-subscription", "virtual-network", "Lists private endpoints in a subscription.")]
[CommandExample("Lists all private endpoints in a subscription",
    "topaz vnet private-endpoint list-by-subscription --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae")]
internal sealed class ListPrivateEndpointsBySubscriptionCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<ListPrivateEndpointsBySubscriptionCommand.ListPrivateEndpointsBySubscriptionCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, ListPrivateEndpointsBySubscriptionCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.Network/privateEndpoints";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, ListPrivateEndpointsBySubscriptionCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListPrivateEndpointsBySubscriptionCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
