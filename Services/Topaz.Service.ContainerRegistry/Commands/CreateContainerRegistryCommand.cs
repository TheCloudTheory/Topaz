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
[CommandDefinition("acr create", "container-registry", "Creates a new Azure Container Registry.")]
[CommandExample("Create a Basic registry", "topaz acr create \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"my-rg\" \\\n    --name \"myregistry\" \\\n    --location \"westeurope\"")]
[CommandExample("Create a Standard registry with admin user", "topaz acr create \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"my-rg\" \\\n    --name \"myregistry\" \\\n    --location \"westeurope\" \\\n    --sku Standard \\\n    --admin-enabled")]
public sealed class CreateContainerRegistryCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<CreateContainerRegistryCommand.CreateContainerRegistryCommandSettings>
{
    public override int Execute(CommandContext context, CreateContainerRegistryCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

        var request = new CreateOrUpdateContainerRegistryRequest
        {
            Location = settings.Location!,
            Tags = settings.Tags?.ToDictionary(t => t.Split('=')[0], t => t.Split('=')[1]),
            Sku = new CreateOrUpdateContainerRegistryRequest.ContainerRegistrySku { Name = settings.Sku },
            Properties = new CreateOrUpdateContainerRegistryRequest.ContainerRegistryProperties
            {
                AdminUserEnabled = settings.AdminEnabled
            }
        };

        var operation = controlPlane.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, settings.Name!, request);
        if (operation.Result is not (OperationResult.Created or OperationResult.Updated))
        {
            Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        AnsiConsole.WriteLine(operation.Resource!.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateContainerRegistryCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Registry name can't be null.");

        if (string.IsNullOrEmpty(settings.Location))
            return ValidationResult.Error("Location can't be null.");

        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");

        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateContainerRegistryCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Registry name (5-50 alphanumeric characters).")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Registry location.")]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("SKU name: Basic, Standard, or Premium. Defaults to Basic.")]
        [CommandOption("--sku")]
        public string? Sku { get; set; } = "Basic";

        [CommandOptionDefinition("Enable the admin user.")]
        [CommandOption("--admin-enabled")]
        public bool? AdminEnabled { get; set; }

        [CommandOptionDefinition("Resource tags (key=value pairs).")]
        [CommandOption("--tags")]
        public string[]? Tags { get; set; }
    }
}
