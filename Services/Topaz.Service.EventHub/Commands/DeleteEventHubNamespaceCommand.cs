using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.EventHub.Commands;

[UsedImplicitly]
[CommandDefinition("eventhubs namespace delete",  "event-hub", "Deletes an Event Hub.")]
[CommandExample("Deletes Event Hub", "topaz eventhubs namespace delete \\\n    --name \"sb-namespace\" \\\n    --resource-group \"rg\" \\\n    --subscription-id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\"")]
public class DeleteEventHubNamespaceCommand(HttpClient httpClient, DefaultsProvider provider) : TopazHttpCommand<DeleteEventHubNamespaceCommand.DeleteEventHubNamespaceCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, DeleteEventHubNamespaceCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.EventHub/namespaces/{settings.Name}";
        if (!await DeleteAsync(url)) return 1;
        AnsiConsole.WriteLine($"Event Hub namespace '{settings.Name}' deleted.");
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, DeleteEventHubNamespaceCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
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
        
        return string.IsNullOrEmpty(settings.Name) 
            ? ValidationResult.Error("Azure Event Hub Namespace name can't be null.") : base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class DeleteEventHubNamespaceCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Event Hub namespace name.")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
        
        [CommandOptionDefinition("(Required) Event Hub namespace resource group name.")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
        
        [CommandOptionDefinition("(Required) Event Hub namespace subscription ID.")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; } = null!;
    }
}