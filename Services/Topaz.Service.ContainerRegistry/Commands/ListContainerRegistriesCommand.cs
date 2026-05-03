using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Commands;

[UsedImplicitly]
[CommandDefinition("acr list", "container-registry", "Lists Azure Container Registries.")]
[CommandExample("List registries in a resource group", "topaz acr list \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"my-rg\"")]
[CommandExample("List all registries in a subscription", "topaz acr list \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\"")]
public sealed class ListContainerRegistriesCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<ListContainerRegistriesCommand.ListContainerRegistriesCommandSettings>
{
    public override int Execute(CommandContext context, ListContainerRegistriesCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

        if (!string.IsNullOrEmpty(settings.ResourceGroup))
        {
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
            var operation = controlPlane.ListByResourceGroup(subscriptionIdentifier, resourceGroupIdentifier);

            if (operation.Result != OperationResult.Success)
            {
                Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
                return 1;
            }

            if (operation.Resource == null || operation.Resource.Length == 0)
            {
                AnsiConsole.WriteLine("No registries found in the resource group.");
                return 0;
            }

            foreach (var registry in operation.Resource)
                AnsiConsole.WriteLine(registry.ToString());
        }
        else
        {
            var operation = controlPlane.ListBySubscription(subscriptionIdentifier);

            if (operation.Result != OperationResult.Success)
            {
                Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
                return 1;
            }

            if (operation.Resource == null || operation.Resource.Length == 0)
            {
                AnsiConsole.WriteLine("No registries found in the subscription.");
                return 0;
            }

            foreach (var registry in operation.Resource)
                AnsiConsole.WriteLine(registry.ToString());
        }

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListContainerRegistriesCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListContainerRegistriesCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("Resource group name. Omit to list across the entire subscription.")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}
