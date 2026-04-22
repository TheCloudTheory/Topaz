using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Commands;

[UsedImplicitly]
[CommandDefinition("servicebus queue delete", "service-bus", "Deletes a queue from a Service Bus namespace.")]
[CommandExample("Delete a queue", "topaz servicebus queue delete \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --namespace-name \"sblocal\" \\\n    --queue-name \"myqueue\"")]
public sealed class DeleteServiceBusQueueCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<DeleteServiceBusQueueCommand.DeleteServiceBusQueueCommandSettings>
{
    public override int Execute(CommandContext context, DeleteServiceBusQueueCommandSettings settings)
    {
        logger.LogDebug(nameof(DeleteServiceBusQueueCommand), nameof(Execute), "Executing {0}.{1}.",
            nameof(DeleteServiceBusQueueCommand), nameof(Execute));

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var resourceGroupControlPlane =
            new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger),
                SubscriptionControlPlane.New(eventPipeline, logger), logger);
        var resourceGroup = resourceGroupControlPlane.Get(SubscriptionIdentifier.From(settings.SubscriptionId),
            resourceGroupIdentifier);
        if (resourceGroup.Result == OperationResult.NotFound || resourceGroup.Resource == null)
        {
            logger.LogError($"Resource group {resourceGroupIdentifier} not found.");
            return 1;
        }

        var controlPlane = ServiceBusServiceControlPlane.New(eventPipeline, logger);
        var namespaceIdentifier = ServiceBusNamespaceIdentifier.From(settings.NamespaceName!);
        var @namespace =
            controlPlane.GetNamespace(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier);
        if (@namespace.Result == OperationResult.NotFound || @namespace.Resource == null)
        {
            logger.LogError($"Namespace {namespaceIdentifier} not found.");
            return 1;
        }

        var deleteOperation = controlPlane.DeleteQueue(subscriptionIdentifier, resourceGroupIdentifier,
            namespaceIdentifier, settings.Name!);
        if (deleteOperation.Result == OperationResult.Failed)
        {
            logger.LogError($"There was a problem deleting queue '{settings.Name!}'.");
            return 1;
        }

        logger.LogInformation($"Queue '{settings.Name}' deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteServiceBusQueueCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Service Bus queue name can't be null.");
        }

        if (string.IsNullOrEmpty(settings.NamespaceName))
        {
            return ValidationResult.Error("Service Bus namespace name can't be null.");
        }

        if (string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Service Bus namespace resource group can't be null.");
        }

        if (string.IsNullOrEmpty(settings.SubscriptionId))
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
    public sealed class DeleteServiceBusQueueCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) Queue name.", required: true)]
        [CommandOption("-n|--queue-name")] public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Service Bus namespace name.", required: true)]
        [CommandOption("--namespace-name")] public string? NamespaceName { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
    }
}