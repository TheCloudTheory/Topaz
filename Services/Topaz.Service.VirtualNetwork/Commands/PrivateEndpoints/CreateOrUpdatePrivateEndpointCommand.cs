using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualNetwork.Commands.PrivateEndpoints;

[UsedImplicitly]
[CommandDefinition("vnet private-endpoint create", "virtual-network", "Creates or updates a private endpoint.")]
[CommandExample("Creates a private endpoint",
    "topaz vnet private-endpoint create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-pe\" \\\n    --location \"eastus\" \\\n    --resource-group \"rg-local\"")]
internal sealed class CreateOrUpdatePrivateEndpointCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CreateOrUpdatePrivateEndpointCommand.CreateOrUpdatePrivateEndpointCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CreateOrUpdatePrivateEndpointCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Network/privateEndpoints/{settings.Name}";
        var (success, body) = await PutAsync(url, new { location = settings.Location });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, CreateOrUpdatePrivateEndpointCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Private endpoint name can't be null.");
        if (string.IsNullOrEmpty(settings.Location))
            return ValidationResult.Error("Location can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateOrUpdatePrivateEndpointCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) private endpoint name")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) location")]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
