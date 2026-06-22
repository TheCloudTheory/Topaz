using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualMachine.Commands;

[UsedImplicitly]
[CommandDefinition("vm update", "virtual-machine", "Updates an Azure Virtual Machine.")]
[CommandExample("Updates a Virtual Machine",
    "topaz vm update --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-vm\" \\\n    --resource-group \"rg-local\" \\\n    --size \"Standard_D2_v3\"")]
internal sealed class UpdateVirtualMachineCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<UpdateVirtualMachineCommand.UpdateVirtualMachineCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, UpdateVirtualMachineCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Compute/virtualMachines/{settings.Name}";
        
        var updatePayload = new Dictionary<string, object>();
        
        if (!string.IsNullOrEmpty(settings.Size))
        {
            updatePayload["properties"] = new
            {
                hardwareProfile = new { vmSize = settings.Size }
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

    public override ValidationResult Validate(CommandContext context, UpdateVirtualMachineCommandSettings settings)
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
    public sealed class UpdateVirtualMachineCommandSettings : CommandSettings
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

        [CommandOptionDefinition("(Optional) Virtual machine size.", required: false)]
        [CommandOption("--size")]
        public string? Size { get; set; }
    }
}
