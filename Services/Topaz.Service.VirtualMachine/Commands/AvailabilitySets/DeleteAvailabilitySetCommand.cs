using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualMachine.Commands.AvailabilitySets;

[UsedImplicitly]
[CommandDefinition("availability-set delete", "virtual-machine", "Deletes an Azure Availability Set.")]
[CommandExample("Deletes an Availability Set",
    "topaz availability-set delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-avset\" \\\n    --resource-group \"rg-local\"")]
internal sealed class DeleteAvailabilitySetCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<DeleteAvailabilitySetCommand.DeleteAvailabilitySetCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, DeleteAvailabilitySetCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Compute/availabilitySets/{settings.Name}";
        if (!await DeleteAsync(url)) return 1;
        AnsiConsole.WriteLine($"Availability set '{settings.Name}' deleted.");
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, DeleteAvailabilitySetCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Availability set name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class DeleteAvailabilitySetCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Availability set name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
