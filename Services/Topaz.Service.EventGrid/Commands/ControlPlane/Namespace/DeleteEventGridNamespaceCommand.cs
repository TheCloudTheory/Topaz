using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.EventGrid.Commands.ControlPlane.Namespace;

[UsedImplicitly]
[CommandDefinition("eventgrid namespace delete", "event-grid", "Deletes an Event Grid Namespace.")]
[CommandExample("Delete an Event Grid Namespace", "topaz eventgrid namespace delete \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"my-namespace\"")]
public sealed class DeleteEventGridNamespaceCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<DeleteEventGridNamespaceCommand.DeleteEventGridNamespaceCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, DeleteEventGridNamespaceCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.EventGrid/namespaces/{settings.Name}";
        if (!await DeleteAsync(url)) return 1;
        AnsiConsole.WriteLine($"Event Grid Namespace '{settings.Name}' deleted.");
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, DeleteEventGridNamespaceCommandSettings settings)
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
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class DeleteEventGridNamespaceCommandSettings : CommandSettings
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
    }
}
