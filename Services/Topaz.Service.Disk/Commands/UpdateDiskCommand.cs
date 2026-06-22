using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Disk.Commands;

[UsedImplicitly]
[CommandDefinition("disk update", "managed-disk", "Updates an Azure Managed Disk (tags, diskSizeGB, SKU).")]
[CommandExample("Updates a Managed Disk's tags",
    "topaz disk update --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-disk\" \\\n    --resource-group \"rg-local\" \\\n    --tags env=test")]
internal sealed class UpdateDiskCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<UpdateDiskCommand.UpdateDiskCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, UpdateDiskCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Compute/disks/{settings.Name}";

        var tags = settings.Tags?
            .Select(t => t.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0], p => p[1]);

        var body = new Dictionary<string, object?>();
        if (tags != null) body["tags"] = tags;
        if (settings.Sku != null) body["sku"] = new { name = settings.Sku };
        if (settings.DiskSizeGB.HasValue)
            body["properties"] = new { diskSizeGB = settings.DiskSizeGB.Value };

        var (success, responseBody) = await PatchAsync(url, body);
        if (!success) return 1;
        AnsiConsole.WriteLine(responseBody);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, UpdateDiskCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Disk name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class UpdateDiskCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Disk name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Optional) Space-separated tags in key=value format.", required: false)]
        [CommandOption("--tags")]
        public string[]? Tags { get; set; }

        [CommandOptionDefinition("(Optional) SKU name (e.g. Premium_LRS, Standard_LRS).", required: false)]
        [CommandOption("--sku")]
        public string? Sku { get; set; }

        [CommandOptionDefinition("(Optional) New disk size in GB.", required: false)]
        [CommandOption("--disk-size-gb")]
        public long? DiskSizeGB { get; set; }
    }
}
