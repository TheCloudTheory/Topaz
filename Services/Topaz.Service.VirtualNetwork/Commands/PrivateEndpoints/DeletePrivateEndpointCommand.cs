using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualNetwork.Commands.PrivateEndpoints;

[UsedImplicitly]
[CommandDefinition("vnet private-endpoint delete", "virtual-network", "Deletes a private endpoint.")]
[CommandExample("Deletes a private endpoint",
    "topaz vnet private-endpoint delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-pe\" \\\n    --resource-group \"rg-local\"")]
internal sealed class DeletePrivateEndpointCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<DeletePrivateEndpointCommand.DeletePrivateEndpointCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, DeletePrivateEndpointCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Network/privateEndpoints/{settings.Name}";
        if (!await DeleteAsync(url)) return 1;
        AnsiConsole.WriteLine($"Private endpoint '{settings.Name}' deleted.");
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, DeletePrivateEndpointCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Private endpoint name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class DeletePrivateEndpointCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) private endpoint name")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
