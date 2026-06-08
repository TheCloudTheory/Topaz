using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Disk.Commands;

[UsedImplicitly]
[CommandDefinition("disk grant-access", "managed-disk", "Grants SAS access to an Azure Managed Disk.")]
[CommandExample("Grant read access to a Managed Disk",
    "topaz disk grant-access --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-disk\" \\\n    --resource-group \"rg-local\" \\\n    --access \"Read\" \\\n    --duration-in-seconds 3600")]
internal sealed class GrantDiskAccessCommand(HttpClient httpClient)
    : TopazHttpCommand<GrantDiskAccessCommand.GrantDiskAccessCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, GrantDiskAccessCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Compute/disks/{settings.Name}/beginGetAccess";
        var (success, body) = await PostAsync(url, new
        {
            access = settings.Access,
            durationInSeconds = settings.DurationInSeconds
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, GrantDiskAccessCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Disk name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (string.IsNullOrEmpty(settings.Access))
            return ValidationResult.Error("Access level can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class GrantDiskAccessCommandSettings : CommandSettings
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

        [CommandOptionDefinition("(Required) Access level: Read, Write, or ReadWrite.", required: true)]
        [CommandOption("--access")]
        public string? Access { get; set; }

        [CommandOptionDefinition("(Required) Duration of SAS access in seconds.", required: true)]
        [CommandOption("--duration-in-seconds")]
        public int DurationInSeconds { get; set; } = 3600;
    }
}
