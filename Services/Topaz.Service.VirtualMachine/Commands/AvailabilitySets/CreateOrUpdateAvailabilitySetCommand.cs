using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualMachine.Commands.AvailabilitySets;

[UsedImplicitly]
[CommandDefinition("availability-set create", "virtual-machine", "Creates or updates an Azure Availability Set.")]
[CommandExample("Creates an Availability Set",
    "topaz availability-set create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-avset\" \\\n    --resource-group \"rg-local\" \\\n    --location \"westeurope\"")]
internal sealed class CreateOrUpdateAvailabilitySetCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CreateOrUpdateAvailabilitySetCommand.CreateOrUpdateAvailabilitySetCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CreateOrUpdateAvailabilitySetCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Compute/availabilitySets/{settings.Name}";
        var (success, body) = await PutAsync(url, new
        {
            location = settings.Location,
            properties = new
            {
                platformFaultDomainCount = settings.PlatformFaultDomainCount,
                platformUpdateDomainCount = settings.PlatformUpdateDomainCount
            }
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, CreateOrUpdateAvailabilitySetCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        settings.Location ??= defaults.Location;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Availability set name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.Location))
            return ValidationResult.Error("Location can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateOrUpdateAvailabilitySetCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Availability set name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Azure region.", required: true)]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Optional) Number of fault domains.", required: false)]
        [CommandOption("--fault-domain-count")]
        public int? PlatformFaultDomainCount { get; set; }

        [CommandOptionDefinition("(Optional) Number of update domains.", required: false)]
        [CommandOption("--update-domain-count")]
        public int? PlatformUpdateDomainCount { get; set; }
    }
}
