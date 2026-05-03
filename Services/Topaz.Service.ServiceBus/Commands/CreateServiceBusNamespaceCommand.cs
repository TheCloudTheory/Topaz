using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.ServiceBus.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Commands;

[UsedImplicitly]
[CommandDefinition("servicebus namespace create", "service-bus", "Creates or updates a Service Bus namespace.")]
[CommandExample("Create a namespace", "topaz servicebus namespace create \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"sblocal\"")]
public sealed class CreateServiceBusNamespaceCommand(Pipeline eventPipeline, ITopazLogger logger) : Command<CreateServiceBusNamespaceCommand.CreateServiceBusNamespaceCommandSettings>
{
    public override int Execute(CommandContext context, CreateServiceBusNamespaceCommandSettings settings)
    {
        logger.LogDebug(nameof(CreateServiceBusNamespaceCommand), nameof(Execute), "Executing {0}.{1}.", nameof(CreateServiceBusNamespaceCommand), nameof(Execute));

        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var resourceGroupControlPlane =
            new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);
        var resourceGroup = resourceGroupControlPlane.Get(SubscriptionIdentifier.From(settings.SubscriptionId), resourceGroupIdentifier);
        if (resourceGroup.Result == OperationResult.NotFound || resourceGroup.Resource == null)
        {
            Console.Error.WriteLine($"ResourceGroup {resourceGroupIdentifier} not found.");
            return 1;
        }

        var namespaceIdentifier = ServiceBusNamespaceIdentifier.From(settings.Name!);
        var controlPlane = ServiceBusServiceControlPlane.New(eventPipeline, logger);
        var request = new CreateOrUpdateServiceBusNamespaceRequest();
        var ns = controlPlane.CreateOrUpdateNamespace(resourceGroup.Resource.GetSubscription(), resourceGroupIdentifier, resourceGroup.Resource.Location, namespaceIdentifier, request);

        if (ns.Result == OperationResult.Failed || ns.Resource == null)
        {
            Console.Error.WriteLine($"There was a problem creating namespace '{namespaceIdentifier}'.");
            return 1;
        }
        
        AnsiConsole.WriteLine(ns.Resource.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateServiceBusNamespaceCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
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
    public sealed class CreateServiceBusNamespaceCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
        
        [CommandOptionDefinition("(Required) Namespace name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}