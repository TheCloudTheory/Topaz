using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualMachine.Commands.AvailabilitySets;

[UsedImplicitly]
[CommandDefinition("availability-set update", "virtual-machine", "Updates an Azure Availability Set.")]
[CommandExample("Updates an Availability Set",
    "topaz availability-set update --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-avset\" \\\n    --resource-group \"rg-local\" \\\n    --fault-domain-count 3")]
internal sealed class UpdateAvailabilitySetCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<UpdateAvailabilitySetCommand.UpdateAvailabilitySetCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, UpdateAvailabilitySetCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Compute/availabilitySets/{settings.Name}";

        var updatePayload = new Dictionary<string, object>();

        if (settings.PlatformFaultDomainCount.HasValue || settings.PlatformUpdateDomainCount.HasValue)
        {
            updatePayload["properties"] = new
            {
                platformFaultDomainCount = settings.PlatformFaultDomainCount,
                platformUpdateDomainCount = settings.PlatformUpdateDomainCount
            };
        }

        if (updatePayload.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Warning: No updates specified.[/]");
            return 0;
        }

        var (success, body) = await PatchAsync(url, updatePayload);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, UpdateAvailabilitySetCommandSettings settings)
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
    public sealed class UpdateAvailabilitySetCommandSettings : CommandSettings
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

        [CommandOptionDefinition("(Optional) Number of fault domains.", required: false)]
        [CommandOption("--fault-domain-count")]
        public int? PlatformFaultDomainCount { get; set; }

        [CommandOptionDefinition("(Optional) Number of update domains.", required: false)]
        [CommandOption("--update-domain-count")]
        public int? PlatformUpdateDomainCount { get; set; }
    }
}
