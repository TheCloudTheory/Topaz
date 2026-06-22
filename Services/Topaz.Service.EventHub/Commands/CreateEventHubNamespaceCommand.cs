using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.EventHub.Commands;

[UsedImplicitly]
[CommandDefinition("eventhubs namespace create",  "event-hub", "Creates new Event Hub namespace.")]
[CommandExample("Creates Event Hub namespace", "topaz eventhubs namespace create \\\n    --resource-group rg-test \\\n    --location \"westeurope\" \\\n    --name \"eh-namespace\" \\\n    --subscription-id \"07CB2605-9C16-46E9-A2BD-0A8D39E049E8\"")]
public sealed class CreateEventHubNamespaceCommand(HttpClient httpClient, DefaultsProvider provider) : TopazHttpCommand<CreateEventHubNamespaceCommand.CreateEventHubCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreateEventHubCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.EventHub/namespaces/{settings.Name}";
        var (success, body) = await PutAsync(url, new { location = settings.Location, properties = new { } });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateEventHubCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        settings.Location ??= defaults.Location;
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Resource group name can't be null.");
        }

        if(string.IsNullOrEmpty(settings.Location))
        {
            return ValidationResult.Error("Event Hub Namespace location can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Event Hub Namespace subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Event Hub Namespace subscription ID must be a valid GUID.");
        }

        return base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class CreateEventHubCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) hub name.")]
        [CommandOption("-n|--name")] public string? Name { get; set; }

        [CommandOptionDefinition("(Required) resource group name.")]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Event Hub namespace location.")]
        [CommandOption("-l|--location")] public string? Location { get; set; }
        
        [CommandOptionDefinition("(Required) subscription ID.")]
        [CommandOption("-s|--subscription-id")] public string SubscriptionId { get; set; } = null!;
    }
}