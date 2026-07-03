using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualMachine.Commands;

[UsedImplicitly]
[CommandDefinition("vm image-version list", "virtual-machine", "Lists Azure Virtual Machine image versions.")]
[CommandExample("Lists Virtual Machine image versions",
    "topaz vm image-version list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --location \"westeurope\" \\\n    --publisher \"Canonical\" \\\n    --offer \"0001-com-ubuntu-server-focal\" \\\n    --sku \"20_04-lts-gen2\"")]
internal sealed class ListVirtualMachineImageVersionsCommand(HttpClient httpClient)
    : TopazHttpCommand<ListVirtualMachineImageVersionsCommand.ListVirtualMachineImageVersionsCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, ListVirtualMachineImageVersionsCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.Compute/locations/{settings.Location}/publishers/{settings.Publisher}/artifacttypes/vmimage/offers/{settings.Offer}/skus/{settings.Sku}/versions";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, ListVirtualMachineImageVersionsCommandSettings settings)
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
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListVirtualMachineImageVersionsCommandSettings : CommandSettings
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
    }
}
