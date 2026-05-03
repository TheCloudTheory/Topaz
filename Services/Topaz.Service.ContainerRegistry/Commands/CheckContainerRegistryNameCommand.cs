using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Commands;

[UsedImplicitly]
[CommandDefinition("acr check-name", "container-registry", "Checks whether a container registry name is available.")]
[CommandExample("Check registry name availability", "topaz acr check-name \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --name \"myregistry\"")]
public sealed class CheckContainerRegistryNameCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<CheckContainerRegistryNameCommand.CheckNameCommandSettings>
{
    public override int Execute(CommandContext context, CheckNameCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

        var isAvailable = controlPlane.IsNameAvailable(subscriptionIdentifier, null, settings.Name!);

        if (isAvailable)
            AnsiConsole.WriteLine($"Name '{settings.Name}' is available.");
        else
            AnsiConsole.WriteLine($"Name '{settings.Name}' is not available.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CheckNameCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Registry name can't be null.");

        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CheckNameCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Registry name to check.")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
