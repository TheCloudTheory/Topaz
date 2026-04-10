using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Commands;

[UsedImplicitly]
[CommandDefinition("acr repository list", "container-registry", "Lists repositories in an Azure Container Registry.")]
[CommandExample("List repositories in a registry", "topaz acr repository list \\\n+    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n+    --resource-group \"my-rg\" \\\n+    --registry \"myregistry\"")]
public sealed class ListRepositoriesCommand(ITopazLogger logger)
    : Command<ListRepositoriesCommand.ListRepositoriesCommandSettings>
{
    public override int Execute(CommandContext context, ListRepositoriesCommandSettings settings)
    {
        logger.LogInformation($"Executing {nameof(ListRepositoriesCommand)}.{nameof(Execute)}.");

        var dataPlane = AcrDataPlane();
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);

        var repositories = dataPlane.ListRepositories(subscriptionIdentifier, resourceGroupIdentifier, settings.Registry!);

        if (repositories.Count == 0)
        {
            logger.LogInformation("No repositories found.");
            return 0;
        }

        foreach (var repository in repositories)
            logger.LogInformation(repository);

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListRepositoriesCommandSettings settings)
    {
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
    public sealed class ListRepositoriesCommandSettings : CommandSettings
    {
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