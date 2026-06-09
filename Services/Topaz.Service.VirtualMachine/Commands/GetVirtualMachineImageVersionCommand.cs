using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualMachine.Commands;

[UsedImplicitly]
[CommandDefinition("vm image-version get", "virtual-machine", "Gets an Azure Virtual Machine image version.")]
[CommandExample("Gets a Virtual Machine image version",
    "topaz vm image-version get --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --location \"westeurope\" \\\n    --publisher \"Canonical\" \\\n    --offer \"0001-com-ubuntu-server-focal\" \\\n    --sku \"20_04-lts-gen2\" \\\n    --version \"20.04.202208100\"")]
internal sealed class GetVirtualMachineImageVersionCommand(HttpClient httpClient)
    : TopazHttpCommand<GetVirtualMachineImageVersionCommand.GetVirtualMachineImageVersionCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, GetVirtualMachineImageVersionCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.Compute/locations/{settings.Location}/publishers/{settings.Publisher}/artifacttypes/vmimage/offers/{settings.Offer}/skus/{settings.Sku}/versions/{settings.Version}";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, GetVirtualMachineImageVersionCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (string.IsNullOrEmpty(settings.Location))
            return ValidationResult.Error("Location can't be null.");
        if (string.IsNullOrEmpty(settings.Publisher))
            return ValidationResult.Error("Publisher can't be null.");
        if (string.IsNullOrEmpty(settings.Offer))
            return ValidationResult.Error("Offer can't be null.");
        if (string.IsNullOrEmpty(settings.Sku))
            return ValidationResult.Error("SKU can't be null.");
        if (string.IsNullOrEmpty(settings.Version))
            return ValidationResult.Error("Version can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class GetVirtualMachineImageVersionCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Required) Location.", required: true)]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Required) Publisher.", required: true)]
        [CommandOption("-p|--publisher")]
        public string? Publisher { get; set; }

        [CommandOptionDefinition("(Required) Offer.", required: true)]
        [CommandOption("-o|--offer")]
        public string? Offer { get; set; }

        [CommandOptionDefinition("(Required) SKU.", required: true)]
        [CommandOption("--sku")]
        public string? Sku { get; set; }

        [CommandOptionDefinition("(Required) Version.", required: true)]
        [CommandOption("-v|--version")]
        public string? Version { get; set; }
    }
}
