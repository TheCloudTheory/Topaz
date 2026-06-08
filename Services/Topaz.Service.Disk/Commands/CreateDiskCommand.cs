using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Disk.Commands;

[UsedImplicitly]
[CommandDefinition("disk create", "managed-disk", "Creates or updates an Azure Managed Disk.")]
[CommandExample("Creates a new Managed Disk",
    "topaz disk create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-disk\" \\\n    --location \"westeurope\" \\\n    --resource-group \"rg-local\"")]
internal sealed class CreateDiskCommand(HttpClient httpClient)
    : TopazHttpCommand<CreateDiskCommand.CreateDiskCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreateDiskCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Compute/disks/{settings.Name}";
        var (success, body) = await PutAsync(url, new
        {
            location = settings.Location,
            sku = settings.Sku == null ? null : new { name = settings.Sku },
            properties = new
            {
                diskSizeGB = settings.DiskSizeGB,
                creationData = new { createOption = "Empty" }
            }
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateDiskCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Disk name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.Location))
            return ValidationResult.Error("Location can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateDiskCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Disk name.", required: true)]
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

        [CommandOptionDefinition("(Optional) Disk size in GB. Defaults to 32.", required: false)]
        [CommandOption("--disk-size-gb")]
        public long DiskSizeGB { get; set; } = 32;

        [CommandOptionDefinition("(Optional) SKU name (e.g. Premium_LRS, Standard_LRS).", required: false)]
        [CommandOption("--sku")]
        public string? Sku { get; set; }
    }
}
