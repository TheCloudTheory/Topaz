using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.EventGrid.Commands.ControlPlane.Topic;

[UsedImplicitly]
[CommandDefinition("eventgrid topic regenerate-key", "event-grid", "Regenerates an access key for an Event Grid Topic.")]
[CommandExample("Regenerate the primary key for an Event Grid Topic", "topaz eventgrid topic regenerate-key \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"my-topic\" \\\n    --key-name \"key1\"")]
public sealed class RegenerateEventGridTopicKeyCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<RegenerateEventGridTopicKeyCommand.RegenerateEventGridTopicKeyCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, RegenerateEventGridTopicKeyCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.EventGrid/topics/{settings.Name}/regenerateKey";
        var (success, body) = await PostAsync(url, new { keyName = settings.KeyName });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, RegenerateEventGridTopicKeyCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;

        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Topic name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        var validKeyNames = new[] { "key1", "key2" };
        if (!validKeyNames.Contains(settings.KeyName, StringComparer.OrdinalIgnoreCase))
            return ValidationResult.Error("Key name must be 'key1' or 'key2'.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class RegenerateEventGridTopicKeyCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Required) Event Grid Topic name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Key name to regenerate: key1 or key2.", required: true)]
        [CommandOption("-k|--key-name")]
        public string KeyName { get; set; } = null!;
    }
}
