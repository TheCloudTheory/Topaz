using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualMachine.Commands;

[UsedImplicitly]
[CommandDefinition("vm show", "virtual-machine", "Gets an Azure Virtual Machine.")]
[CommandExample("Gets a Virtual Machine",
    "topaz vm show --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-vm\" \\\n    --resource-group \"rg-local\"")]
internal sealed class GetVirtualMachineCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<GetVirtualMachineCommand.GetVirtualMachineCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, GetVirtualMachineCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Compute/virtualMachines/{settings.Name}";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, GetVirtualMachineCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Virtual machine name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class GetVirtualMachineCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Virtual machine name.", required: true)]
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
