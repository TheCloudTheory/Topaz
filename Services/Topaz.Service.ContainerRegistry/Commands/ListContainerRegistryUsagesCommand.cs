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
[CommandDefinition("acr list-usages", "container-registry", "Lists quota usages for an Azure Container Registry.")]
[CommandExample("List usages for a registry", "topaz acr list-usages \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"my-rg\" \\\n    --name \"myregistry\"")]
public sealed class ListContainerRegistryUsagesCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<ListContainerRegistryUsagesCommand.ListUsagesCommandSettings>
{
    public override int Execute(CommandContext context, ListUsagesCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

        var operation = controlPlane.ListUsages(subscriptionIdentifier, resourceGroupIdentifier, settings.Name!);
        if (operation.Result == OperationResult.NotFound)
        {
            Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        foreach (var usage in operation.Resource!)
            AnsiConsole.WriteLine($"{usage.Name}: {usage.CurrentValue} / {usage.Limit} {usage.Unit}");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListUsagesCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Registry name can't be null.");

        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");

        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListUsagesCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Registry name.")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
