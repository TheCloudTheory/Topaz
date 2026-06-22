using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.LoadBalancer.Commands;

[UsedImplicitly]
[CommandDefinition("lb update", "load-balancer", "Updates an Azure Load Balancer (tags).")]
[CommandExample("Updates a Load Balancer's tags",
    "topaz lb update --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-lb\" \\\n    --resource-group \"rg-local\" \\\n    --tags env=test")]
internal sealed class UpdateLoadBalancerCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<UpdateLoadBalancerCommand.UpdateLoadBalancerCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, UpdateLoadBalancerCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Network/loadBalancers/{settings.Name}";

        var tags = settings.Tags?
            .Select(t => t.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0], p => p[1]);

        var body = new Dictionary<string, object?>();
        if (tags != null) body["tags"] = tags;

        var (success, responseBody) = await PatchAsync(url, body);
        if (!success) return 1;
        AnsiConsole.WriteLine(responseBody);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, UpdateLoadBalancerCommandSettings settings)
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
    public sealed class UpdateLoadBalancerCommandSettings : CommandSettings
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

        [CommandOptionDefinition("(Optional) Space-separated tags in key=value format.", required: false)]
        [CommandOption("--tags")]
        public string[]? Tags { get; set; }
    }
}
