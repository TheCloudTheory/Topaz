using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.LoadBalancer.Commands;

[UsedImplicitly]
[CommandDefinition("lb show", "load-balancer", "Gets an Azure Load Balancer.")]
[CommandExample("Gets a Load Balancer",
    "topaz lb show --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-lb\" \\\n    --resource-group \"rg-local\"")]
internal sealed class GetLoadBalancerCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<GetLoadBalancerCommand.GetLoadBalancerCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, GetLoadBalancerCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Network/loadBalancers/{settings.Name}";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, GetLoadBalancerCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Load Balancer name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class GetLoadBalancerCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Load Balancer name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
