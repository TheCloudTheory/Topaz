using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Commands;

[UsedImplicitly]
[CommandDefinition("servicebus namespace delete", "service-bus", "Deletes a Service Bus namespace.")]
[CommandExample("Delete a namespace", "topaz servicebus namespace delete \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"sblocal\"")]
public sealed class DeleteServiceBusNamespaceCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<DeleteServiceBusNamespaceCommand.DeleteServiceBusNamespaceCommandSettings>
{
    public override int Execute(CommandContext context, DeleteServiceBusNamespaceCommandSettings settings)
    {
        logger.LogDebug(nameof(DeleteServiceBusNamespaceCommand), nameof(Execute), "Executing {0}.{1}.",
            nameof(DeleteServiceBusNamespaceCommand), nameof(Execute));

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var serviceBusNamespaceIdentifier = ServiceBusNamespaceIdentifier.From(settings.Name!);
        var controlPlane = ServiceBusServiceControlPlane.New(eventPipeline, logger);

        _ = controlPlane.DeleteNamespace(subscriptionIdentifier, resourceGroupIdentifier,
            serviceBusNamespaceIdentifier);

        AnsiConsole.WriteLine("Service Bus namespace deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteServiceBusNamespaceCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
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
    public sealed class DeleteServiceBusNamespaceCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Namespace name.", required: true)]
        [CommandOption("-n|--name")] public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
    }
}