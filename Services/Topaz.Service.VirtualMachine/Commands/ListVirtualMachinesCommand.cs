using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualMachine.Commands;

[UsedImplicitly]
[CommandDefinition("vm list", "virtual-machine", "Lists Azure Virtual Machines in a subscription or resource group.")]
[CommandExample("Lists Virtual Machines in a resource group",
    "topaz vm list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --resource-group \"rg-local\"")]
internal sealed class ListVirtualMachinesCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<ListVirtualMachinesCommand.ListVirtualMachinesCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, ListVirtualMachinesCommandSettings settings)
    {
        string url;
        if (!string.IsNullOrWhiteSpace(settings.ResourceGroup))
            url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Compute/virtualMachines";
        else
            url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.Compute/virtualMachines";

        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListVirtualMachinesCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListVirtualMachinesCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Optional) Filter by resource group name.", required: false)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}
