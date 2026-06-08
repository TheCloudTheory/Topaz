using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Disk.Commands;

[UsedImplicitly]
[CommandDefinition("disk revoke-access", "managed-disk", "Revokes SAS access from an Azure Managed Disk.")]
[CommandExample("Revoke SAS access from a Managed Disk",
    "topaz disk revoke-access --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-disk\" \\\n    --resource-group \"rg-local\"")]
internal sealed class RevokeDiskAccessCommand(HttpClient httpClient)
    : TopazHttpCommand<RevokeDiskAccessCommand.RevokeDiskAccessCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, RevokeDiskAccessCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Compute/disks/{settings.Name}/endGetAccess";
        var (success, _) = await PostAsync(url, new { });
        if (!success) return 1;
        AnsiConsole.WriteLine($"SAS access revoked for disk '{settings.Name}'.");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, RevokeDiskAccessCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Disk name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class RevokeDiskAccessCommandSettings : CommandSettings
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
    }
}
