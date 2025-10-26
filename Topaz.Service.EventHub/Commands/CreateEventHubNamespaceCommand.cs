using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.EventHub.Models.Requests;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Commands;

[UsedImplicitly]
[CommandDefinition("eventhubs namespace create",  "event-hub", "Creates new Event Hub namespace.")]
[CommandExample("Creates Event Hub namespace", "topaz eventhubs namespace create \\\n    --resource-group rg-test \\\n    --location \"westeurope\" \\\n    --name \"eh-namespace\" \\\n    --subscription-id \"07CB2605-9C16-46E9-A2BD-0A8D39E049E8\"")]
public sealed class CreateEventHubNamespaceCommand(ITopazLogger logger) : Command<CreateEventHubNamespaceCommand.CreateEventHubCommandSettings>
{
    public override int Execute(CommandContext context, CreateEventHubCommandSettings settings)
    {
        logger.LogDebug($"Executing {nameof(CreateEventHubNamespaceCommand)}.{nameof(Execute)}.");
        
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var resourceGroupControlPlane =
            new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), logger);
        var resourceGroup = resourceGroupControlPlane.Get(SubscriptionIdentifier.From(Guid.Parse(settings.SubscriptionId)), resourceGroupIdentifier);
        if (resourceGroup.result == OperationResult.NotFound || resourceGroup.resource == null)
        {
            logger.LogError($"ResourceGroup {resourceGroupIdentifier} not found.");
            return 1;
        }

        var controlPlane = new EventHubServiceControlPlane(new ResourceProvider(logger), logger);
        var request = new CreateOrUpdateEventHubNamespaceRequest();
        var ns = controlPlane.CreateOrUpdateNamespace(resourceGroup.resource.GetSubscription(), resourceGroupIdentifier,
            settings.Location!, EventHubNamespaceIdentifier.From(settings.Name!), request);

        logger.LogInformation(ns.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateEventHubCommandSettings settings)
    {
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