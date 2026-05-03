using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Commands;

[UsedImplicitly]
[CommandDefinition("acr repository delete", "container-registry", "Deletes a repository from an Azure Container Registry.")]
[CommandExample("Delete a repository", "topaz acr repository delete \\\n+    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n+    --resource-group \"my-rg\" \\\n+    --registry \"myregistry\" \\\n+    --name \"sample-repository\"")]
public sealed class DeleteRepositoryCommand(ITopazLogger logger)
    : Command<DeleteRepositoryCommand.DeleteRepositoryCommandSettings>
{
    public override int Execute(CommandContext context, DeleteRepositoryCommandSettings settings)
    {
        var dataPlane = AcrDataPlane();
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);

        var deleted = dataPlane.DeleteRepository(
            subscriptionIdentifier,
            resourceGroupIdentifier,
            settings.Registry!,
            settings.Name!);

        if (!deleted)
        {
            Console.Error.WriteLine($"Repository '{settings.Name}' not found.");
            return 1;
        }

        AnsiConsole.WriteLine($"Repository '{settings.Name}' deleted.");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteRepositoryCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Repository name can't be null.");

        if (string.IsNullOrEmpty(settings.Registry))
            return ValidationResult.Error("Registry name can't be null.");

        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");

        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        return base.Validate(context, settings);
    }

    private AcrDataPlane AcrDataPlane() =>
        new(new ContainerRegistryResourceProvider(logger), logger);

    [UsedImplicitly]
    public sealed class DeleteRepositoryCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Repository name.")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Registry name.")]
        [CommandOption("-r|--registry")]
        public string? Registry { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}