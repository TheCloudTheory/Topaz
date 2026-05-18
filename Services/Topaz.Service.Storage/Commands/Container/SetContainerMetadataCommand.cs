using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage container metadata set", "azure-storage/container", "Sets metadata key-value pairs on a blob container.")]
[CommandExample("Set container metadata", "topaz storage container metadata set \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --name \"mycontainer\" \\\n    --metadata \"env=prod\" \"owner=team\"")]
public sealed class SetContainerMetadataCommand(HttpClient httpClient)
    : TopazHttpCommand<SetContainerMetadataCommand.SetContainerMetadataCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, SetContainerMetadataCommandSettings settings)
    {
        AnsiConsole.WriteLine("Setting container metadata...");

        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Storage/storageAccounts/{settings.AccountName}/blobServices/default/containers/{settings.Name}";
        var metadata = settings.Metadata?.ToDictionary(
            p => p.Split('=')[0].Trim(),
            p => p.Contains('=') ? p.Split('=', 2)[1].Trim() : string.Empty);
        var (success, body) = await PatchAsync(url, new { properties = new { metadata } });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, SetContainerMetadataCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Container name can't be null.");
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class SetContainerMetadataCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Container name.", required: true)]
        [CommandOption("-n|--name")] public string? Name { get; set; }
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("Metadata key=value pairs (e.g. --metadata \"env=prod\" \"owner=team\").")]
        [CommandOption("--metadata")] public string[]? Metadata { get; set; }
    }
}
