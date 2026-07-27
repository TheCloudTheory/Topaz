using System.Text.Json.Nodes;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Insights.Commands;

[UsedImplicitly]
[CommandDefinition("insights component query", "application-insights", "Queries an Application Insights component.")]
internal sealed class QueryComponentCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<QueryComponentCommand.QueryComponentCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, QueryComponentCommandSettings settings, CancellationToken cancellationToken)
    {
        var ikey =  await GetInstrumentationKey(settings, cancellationToken);
        if (!ikey.success)
        {
            return 1;
        }

        var url = ApplicationInsightsQueryUrl(settings.Name!, ikey.instrumentationKey!);
        var (success, body) = await PostAsync(url, new
        {
            query = settings.Query,
        }, cancellationToken);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    private async Task<(bool success, string? instrumentationKey)> GetInstrumentationKey(QueryComponentCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/microsoft.insights/components/{settings.Name}";
        var (success, body) = await GetAsync(url, cancellationToken);
        if (!success) return (false, null);
        
        var ikey = JsonNode.Parse(body)!["properties"]!["InstrumentationKey"]?.GetValue<string>();
        return string.IsNullOrWhiteSpace(ikey) ? (false, null) : (true, ikey);
    }

    protected override ValidationResult Validate(CommandContext context, QueryComponentCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Component name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (string.IsNullOrEmpty(settings.Query))
            return ValidationResult.Error("Query can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class QueryComponentCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) component name")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
        
        [CommandOptionDefinition("(Required) query to be executed")]
        [CommandOption("--query")]
        public string? Query { get; set; }
    }
}