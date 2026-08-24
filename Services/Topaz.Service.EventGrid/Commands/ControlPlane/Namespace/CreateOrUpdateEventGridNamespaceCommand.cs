using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.EventGrid.Commands.ControlPlane.Namespace;

[UsedImplicitly]
[CommandDefinition("eventgrid namespace create", "event-grid", "Creates or updates an Event Grid Namespace.")]
[CommandExample("Create an Event Grid Namespace", "topaz eventgrid namespace create \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"my-namespace\" \\\n    --location \"westeurope\"")]
public sealed class CreateOrUpdateEventGridNamespaceCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CreateOrUpdateEventGridNamespaceCommand.CreateOrUpdateEventGridNamespaceCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CreateOrUpdateEventGridNamespaceCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.EventGrid/namespaces/{settings.Name}";
        var (success, body) = await PutAsync(url, new
        {
            location = settings.Location,
            sku = settings.SkuName is not null ? new { name = settings.SkuName, capacity = settings.SkuCapacity } : null,
            properties = new { }
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, CreateOrUpdateEventGridNamespaceCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        settings.Location ??= defaults.Location;

        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Namespace name can't be null.");
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
    public sealed class CreateOrUpdateEventGridNamespaceCommandSettings : CommandSettings
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

        [CommandOptionDefinition("(Required) Azure region.", required: true)]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Optional) SKU name (e.g. Standard).", required: false)]
        [CommandOption("--sku-name")]
        public string? SkuName { get; set; }

        [CommandOptionDefinition("(Optional) SKU capacity.", required: false)]
        [CommandOption("--sku-capacity")]
        public int? SkuCapacity { get; set; }
    }
}
