using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualMachine.Commands.AvailabilitySets;

[UsedImplicitly]
[CommandDefinition("availability-set list-by-subscription", "virtual-machine", "Lists all Azure Availability Sets in a subscription.")]
[CommandExample("Lists Availability Sets in a subscription",
    "topaz availability-set list-by-subscription --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae")]
internal sealed class ListAvailabilitySetsBySubscriptionCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<ListAvailabilitySetsBySubscriptionCommand.ListAvailabilitySetsBySubscriptionCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, ListAvailabilitySetsBySubscriptionCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.Compute/availabilitySets";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, ListAvailabilitySetsBySubscriptionCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListAvailabilitySetsBySubscriptionCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
