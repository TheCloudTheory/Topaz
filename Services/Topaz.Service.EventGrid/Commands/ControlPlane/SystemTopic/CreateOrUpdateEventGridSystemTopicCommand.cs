using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.EventGrid.Commands.ControlPlane.SystemTopic;

[UsedImplicitly]
[CommandDefinition("eventgrid system-topic create", "event-grid", "Creates or updates an Event Grid System Topic.")]
[CommandExample("Create an Event Grid System Topic", "topaz eventgrid system-topic create \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"my-system-topic\" \\\n    --location \"westeurope\" \\\n    --source \"/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-local/providers/Microsoft.Storage/storageAccounts/mystorage\" \\\n    --topic-type \"Microsoft.Storage.StorageAccounts\"")]
public sealed class CreateOrUpdateEventGridSystemTopicCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CreateOrUpdateEventGridSystemTopicCommand.CreateOrUpdateEventGridSystemTopicCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CreateOrUpdateEventGridSystemTopicCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.EventGrid/systemTopics/{settings.Name}";
        var (success, body) = await PutAsync(url, new
        {
            location = settings.Location,
            properties = new
            {
                source = settings.Source,
                topicType = settings.TopicType
            }
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, CreateOrUpdateEventGridSystemTopicCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        settings.Location ??= defaults.Location;

        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("System Topic name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.Location))
            return ValidationResult.Error("Location can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateOrUpdateEventGridSystemTopicCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Required) Event Grid System Topic name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Azure region.", required: true)]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Optional) Resource ID of the source of events.", required: false)]
        [CommandOption("--source")]
        public string? Source { get; set; }

        [CommandOptionDefinition("(Optional) Topic type of the source.", required: false)]
        [CommandOption("--topic-type")]
        public string? TopicType { get; set; }
    }
}
