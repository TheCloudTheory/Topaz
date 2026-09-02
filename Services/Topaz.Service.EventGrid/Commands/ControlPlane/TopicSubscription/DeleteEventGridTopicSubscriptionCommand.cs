using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.EventGrid.Commands.ControlPlane.TopicSubscription;

[UsedImplicitly]
[CommandDefinition("eventgrid topic subscription delete", "event-grid", "Deletes an Event Grid Topic event subscription.")]
[CommandExample("Delete an Event Grid Topic event subscription", "topaz eventgrid topic subscription delete \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --topic-name \"my-topic\" \\\n    --name \"my-subscription\"")]
public sealed class DeleteEventGridTopicSubscriptionCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<DeleteEventGridTopicSubscriptionCommand.DeleteEventGridTopicSubscriptionCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, DeleteEventGridTopicSubscriptionCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.EventGrid/topics/{settings.TopicName}/eventSubscriptions/{settings.Name}";
        if (!await DeleteAsync(url)) return 1;
        AnsiConsole.WriteLine($"Event Grid Topic event subscription '{settings.Name}' deleted.");
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, DeleteEventGridTopicSubscriptionCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;

        if (string.IsNullOrEmpty(settings.TopicName))
            return ValidationResult.Error("Topic name can't be null.");
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Event subscription name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class DeleteEventGridTopicSubscriptionCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Required) Event Grid Topic name.", required: true)]
        [CommandOption("-t|--topic-name")]
        public string? TopicName { get; set; }

        [CommandOptionDefinition("(Required) Event subscription name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}
