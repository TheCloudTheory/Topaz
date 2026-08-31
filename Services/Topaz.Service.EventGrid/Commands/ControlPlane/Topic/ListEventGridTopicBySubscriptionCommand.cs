using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.EventGrid.Commands.ControlPlane.Topic;

[UsedImplicitly]
[CommandDefinition("eventgrid topic list-by-subscription", "event-grid", "Lists Event Grid Topics in a subscription.")]
[CommandExample("List Event Grid Topics in a subscription", "topaz eventgrid topic list-by-subscription \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\"")]
public sealed class ListEventGridTopicBySubscriptionCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<ListEventGridTopicBySubscriptionCommand.ListEventGridTopicBySubscriptionCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, ListEventGridTopicBySubscriptionCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.EventGrid/topics";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, ListEventGridTopicBySubscriptionCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;

        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListEventGridTopicBySubscriptionCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
