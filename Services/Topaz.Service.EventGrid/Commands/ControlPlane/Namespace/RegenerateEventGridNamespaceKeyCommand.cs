using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.EventGrid.Commands.ControlPlane.Namespace;

[UsedImplicitly]
[CommandDefinition("eventgrid namespace regenerate-key", "event-grid", "Regenerates an access key for an Event Grid Namespace.")]
[CommandExample("Regenerate the primary key for an Event Grid Namespace", "topaz eventgrid namespace regenerate-key \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"my-namespace\" \\\n    --key-name \"key1\"")]
public sealed class RegenerateEventGridNamespaceKeyCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<RegenerateEventGridNamespaceKeyCommand.RegenerateEventGridNamespaceKeyCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, RegenerateEventGridNamespaceKeyCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.EventGrid/namespaces/{settings.Name}/regenerateKey";
        var body = new { keyName = settings.KeyName };
        var (success, body2) = await PostAsync(url, body);
        if (!success) return 1;
        AnsiConsole.WriteLine(body2);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, RegenerateEventGridNamespaceKeyCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;

        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Namespace name can't be null.");
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
    public sealed class RegenerateEventGridNamespaceKeyCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Required) Event Grid Namespace name.", required: true)]
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
