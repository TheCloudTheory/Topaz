using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Redis.Commands;

[UsedImplicitly]
[CommandDefinition("redis firewall-rule create", "redis", "Creates or updates a firewall rule for an Azure Cache for Redis instance.")]
[CommandExample("Creates a firewall rule for a Redis cache",
    "topaz redis firewall-rule create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-redis\" \\\n    --resource-group \"rg-local\" \\\n    --rule-name \"allow-office\" \\\n    --start-ip \"10.0.0.1\" \\\n    --end-ip \"10.0.0.100\"")]
internal sealed class CreateOrUpdateFirewallRuleCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CreateOrUpdateFirewallRuleCommand.Settings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Cache/redis/{settings.Name}/firewallRules/{settings.RuleName}";
        var (success, body) = await PutAsync(url, new
        {
            properties = new { startIP = settings.StartIp, endIP = settings.EndIp }
        }, cancellationToken);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, Settings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Cache name can't be null.");
        if (string.IsNullOrEmpty(settings.RuleName))
            return ValidationResult.Error("Rule name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (string.IsNullOrEmpty(settings.StartIp))
            return ValidationResult.Error("Start IP can't be null.");
        if (string.IsNullOrEmpty(settings.EndIp))
            return ValidationResult.Error("End IP can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class Settings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Cache name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Firewall rule name.", required: true)]
        [CommandOption("--rule-name")]
        public string? RuleName { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Required) Start IP address.", required: true)]
        [CommandOption("--start-ip")]
        public string? StartIp { get; set; }

        [CommandOptionDefinition("(Required) End IP address.", required: true)]
        [CommandOption("--end-ip")]
        public string? EndIp { get; set; }
    }
}
