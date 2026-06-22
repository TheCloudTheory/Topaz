using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ContainerRegistry.Commands;

[UsedImplicitly]
[CommandDefinition("acr update", "container-registry", "Updates an Azure Container Registry.")]
[CommandExample("Enable admin user", "topaz acr update \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"my-rg\" \\\n    --name \"myregistry\" \\\n    --admin-enabled true")]
[CommandExample("Change SKU and set tags", "topaz acr update \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"my-rg\" \\\n    --name \"myregistry\" \\\n    --sku Premium \\\n    --tags env=prod team=ops")]
public sealed class UpdateContainerRegistryCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<UpdateContainerRegistryCommand.UpdateContainerRegistryCommandSettings>(httpClient)
{

    public override async Task<int> ExecuteAsync(CommandContext context, UpdateContainerRegistryCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ContainerRegistry/registries/{settings.Name}";
        var (success, body) = await PatchAsync(url, new
        {
            sku = settings.Sku != null ? new { name = settings.Sku } : null,
            tags = (object?)settings.Tags?.ToDictionary(t => t.Split('=')[0], t => t.Split('=')[1]),
            properties = new { adminUserEnabled = settings.AdminEnabled }
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, UpdateContainerRegistryCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
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
