using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.ContainerRegistry.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Commands;

[UsedImplicitly]
[CommandDefinition("acr update", "container-registry", "Updates an Azure Container Registry.")]
[CommandExample("Enable admin user", "topaz acr update \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"my-rg\" \\\n    --name \"myregistry\" \\\n    --admin-enabled true")]
[CommandExample("Change SKU and set tags", "topaz acr update \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"my-rg\" \\\n    --name \"myregistry\" \\\n    --sku Premium \\\n    --tags env=prod team=ops")]
public sealed class UpdateContainerRegistryCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<UpdateContainerRegistryCommand.UpdateContainerRegistryCommandSettings>
{
    public override int Execute(CommandContext context, UpdateContainerRegistryCommandSettings settings)
    {
        logger.LogInformation($"Executing {nameof(UpdateContainerRegistryCommand)}.{nameof(Execute)}.");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

        var existing = controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, settings.Name!);
        if (existing.Result == OperationResult.NotFound)
        {
            logger.LogError($"({existing.Code}) {existing.Reason}");
            return 1;
        }

        var existingResource = existing.Resource!;
        var request = new CreateOrUpdateContainerRegistryRequest
        {
            Location = existingResource.Location,
            Tags = settings.Tags?.ToDictionary(t => t.Split('=')[0], t => t.Split('=')[1])
                   ?? existingResource.Tags,
            Sku = new CreateOrUpdateContainerRegistryRequest.ContainerRegistrySku
            {
                Name = settings.Sku ?? existingResource.Sku?.Name
            },
            Properties = new CreateOrUpdateContainerRegistryRequest.ContainerRegistryProperties
            {
                AdminUserEnabled = settings.AdminEnabled
            }
        };

        var operation = controlPlane.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, settings.Name!, request);
        if (operation.Result is not (OperationResult.Created or OperationResult.Updated))
        {
            logger.LogError($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        logger.LogInformation(operation.Resource!.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, UpdateContainerRegistryCommandSettings settings)
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
    public sealed class UpdateContainerRegistryCommandSettings : CommandSettings
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

        [CommandOptionDefinition("SKU name: Basic, Standard, or Premium.")]
        [CommandOption("--sku")]
        public string? Sku { get; set; }

        [CommandOptionDefinition("Enable or disable the admin user (true/false).")]
        [CommandOption("--admin-enabled")]
        public bool? AdminEnabled { get; set; }

        [CommandOptionDefinition("Resource tags (key=value pairs). Replaces existing tags.")]
        [CommandOption("--tags")]
        public string[]? Tags { get; set; }
    }
}
