using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ServiceBus.Commands;

[UsedImplicitly]
[CommandDefinition("servicebus queue create", "service-bus", "Creates or updates a queue in a Service Bus namespace.")]
[CommandExample("Create a queue", "topaz servicebus queue create \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --namespace-name \"sblocal\" \\\n    --queue-name \"myqueue\"")]
public class CreateServiceBusQueueCommand(HttpClient httpClient, DefaultsProvider provider) : TopazHttpCommand<CreateServiceBusQueueCommand.CreateServiceBusQueueCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreateServiceBusQueueCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ServiceBus/namespaces/{settings.NamespaceName}/queues/{settings.Name}";
        var (success, body) = await PutAsync(url, new { properties = new { } });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateServiceBusQueueCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Service Bus queue name can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.NamespaceName))
        {
            return ValidationResult.Error("Service Bus namespace name can't be null.");
        }

        if(string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Service Bus namespace resource group can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Service Bus subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Service Bus subscription ID must be a valid GUID.");
        }

        return base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class CreateServiceBusQueueCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
        
        [CommandOptionDefinition("(Required) Queue name.", required: true)]
        [CommandOption("-n|--queue-name")]
        public string? Name { get; set; }
        
        [CommandOptionDefinition("(Required) Service Bus namespace name.", required: true)]
        [CommandOption("--namespace-name")]
        public string? NamespaceName { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}