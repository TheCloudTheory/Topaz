using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.EventGrid.Commands.ControlPlane.SystemTopic;

[UsedImplicitly]
[CommandDefinition("eventgrid system-topic list-by-subscription", "event-grid", "Lists Event Grid System Topics in a subscription.")]
[CommandExample("List Event Grid System Topics in a subscription", "topaz eventgrid system-topic list-by-subscription \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\"")]
public sealed class ListEventGridSystemTopicBySubscriptionCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<ListEventGridSystemTopicBySubscriptionCommand.ListEventGridSystemTopicBySubscriptionCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, ListEventGridSystemTopicBySubscriptionCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.EventGrid/systemTopics";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, ListEventGridSystemTopicBySubscriptionCommandSettings settings)
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
    public sealed class ListEventGridSystemTopicBySubscriptionCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
