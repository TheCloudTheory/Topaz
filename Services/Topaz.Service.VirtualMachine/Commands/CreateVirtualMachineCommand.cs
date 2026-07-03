using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualMachine.Commands;

[UsedImplicitly]
[CommandDefinition("vm create", "virtual-machine", "Creates or updates an Azure Virtual Machine.")]
[CommandExample("Creates a new Virtual Machine",
    "topaz vm create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-vm\" \\\n    --location \"westeurope\" \\\n    --resource-group \"rg-local\" \\\n    --size \"Standard_D2_v3\"")]
internal sealed class CreateVirtualMachineCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CreateVirtualMachineCommand.CreateVirtualMachineCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CreateVirtualMachineCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Compute/virtualMachines/{settings.Name}";
        var (success, body) = await PutAsync(url, new
        {
            location = settings.Location,
            properties = new
            {
                hardwareProfile = new { vmSize = settings.Size }
            }
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, CreateVirtualMachineCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        settings.Location ??= defaults.Location;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Virtual machine name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.Location))
            return ValidationResult.Error("Location can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateVirtualMachineCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Virtual machine name.", required: true)]
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

        [CommandOptionDefinition("(Optional) VM size (e.g. Standard_D2_v3). Defaults to Standard_D2_v3.", required: false)]
        [CommandOption("--size")]
        public string Size { get; set; } = "Standard_D2_v3";
    }
}
