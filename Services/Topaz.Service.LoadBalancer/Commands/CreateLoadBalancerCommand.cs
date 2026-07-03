using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.LoadBalancer.Commands;

[UsedImplicitly]
[CommandDefinition("lb create", "load-balancer", "Creates or updates an Azure Load Balancer.")]
[CommandExample("Creates a new Load Balancer",
    "topaz lb create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-lb\" \\\n    --location \"westeurope\" \\\n    --resource-group \"rg-local\"")]
internal sealed class CreateLoadBalancerCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CreateLoadBalancerCommand.CreateLoadBalancerCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CreateLoadBalancerCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Network/loadBalancers/{settings.Name}";
        var (success, body) = await PutAsync(url, new
        {
            location = settings.Location,
            sku = settings.Sku == null ? null : new { name = settings.Sku },
            properties = new { }
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, CreateLoadBalancerCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        settings.Location ??= defaults.Location;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Load Balancer name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.Location))
            return ValidationResult.Error("Location can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateLoadBalancerCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Load Balancer name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Azure region.", required: true)]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Optional) SKU name (e.g. Standard, Basic).", required: false)]
        [CommandOption("--sku")]
        public string? Sku { get; set; }
    }
}
