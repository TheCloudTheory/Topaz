using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.LoadBalancer.Commands;

[UsedImplicitly]
[CommandDefinition("lb list", "load-balancer", "Lists Azure Load Balancers.")]
[CommandExample("Lists Load Balancers in a resource group",
    "topaz lb list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --resource-group \"rg-local\"")]
internal sealed class ListLoadBalancersCommand(HttpClient httpClient)
    : TopazHttpCommand<ListLoadBalancersCommand.ListLoadBalancersCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, ListLoadBalancersCommandSettings settings)
    {
        var url = string.IsNullOrEmpty(settings.ResourceGroup)
            ? $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.Network/loadBalancers"
            : $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Network/loadBalancers";
        
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListLoadBalancersCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListLoadBalancersCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Optional) Resource group name. If omitted, lists all Load Balancers in the subscription.", required: false)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
