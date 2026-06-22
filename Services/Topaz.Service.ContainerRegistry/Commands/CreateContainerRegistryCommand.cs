using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ContainerRegistry.Commands;

[UsedImplicitly]
[CommandDefinition("acr create", "container-registry", "Creates a new Azure Container Registry.")]
[CommandExample("Create a Basic registry", "topaz acr create \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"my-rg\" \\\n    --name \"myregistry\" \\\n    --location \"westeurope\"")]
[CommandExample("Create a Standard registry with admin user", "topaz acr create \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"my-rg\" \\\n    --name \"myregistry\" \\\n    --location \"westeurope\" \\\n    --sku Standard \\\n    --admin-enabled")]
public sealed class CreateContainerRegistryCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CreateContainerRegistryCommand.CreateContainerRegistryCommandSettings>(httpClient)
{

    public override async Task<int> ExecuteAsync(CommandContext context, CreateContainerRegistryCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ContainerRegistry/registries/{settings.Name}";
        var (success, body) = await PutAsync(url, new
        {
            location = settings.Location,
            tags = (object?)settings.Tags?.ToDictionary(t => t.Split('=')[0], t => t.Split('=')[1]),
            sku = new { name = settings.Sku },
            properties = new { adminUserEnabled = settings.AdminEnabled }
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateContainerRegistryCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        settings.Location ??= defaults.Location;
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
