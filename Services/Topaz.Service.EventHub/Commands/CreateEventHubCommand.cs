using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.EventHub.Commands;

[UsedImplicitly]
[CommandDefinition("eventhubs eventhub create",  "event-hub", "Creates new Event Hub.")]
[CommandExample("Creates Event Hub", "topaz eventhubs eventhub create \\\n    --resource-group rg-test \\\n    --namespace-name \"eh-namespace\" \\\n    --name \"hubtest\" \\\n    --subscription-id \"07CB2605-9C16-46E9-A2BD-0A8D39E049E8\"")]
public sealed class CreateEventHubCommand(HttpClient httpClient, DefaultsProvider provider) : TopazHttpCommand<CreateEventHubCommand.CreateEventHubCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreateEventHubCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.EventHub/namespaces/{settings.NamespaceName}/eventhubs/{settings.Name}";
        var (success, body) = await PutAsync(url, new { properties = new { } });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateEventHubCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Event Hub hub name can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.NamespaceName))
        {
            return ValidationResult.Error("Event Hub namespace name can't be null.");
        }

        if(string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Event Hub namespace resource group can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Resource group subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Resource group subscription ID must be a valid GUID.");
        }

        return base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class CreateEventHubCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) hub name.")]
        [CommandOption("-n|--name")] public string? Name { get; set; }
        
        [CommandOptionDefinition("(Required) namespace name.")]
        [CommandOption("--namespace-name")]
        public string? NamespaceName { get; set; }
        
        [CommandOptionDefinition("(Required) resource group name.")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
        
        [CommandOptionDefinition("(Required) subscription ID.")]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
    }
}