using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Disk.Commands;

[UsedImplicitly]
[CommandDefinition("disk list", "managed-disk", "Lists Azure Managed Disks in a subscription or resource group.")]
[CommandExample("Lists Managed Disks in a resource group",
    "topaz disk list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --resource-group \"rg-local\"")]
internal sealed class ListDisksCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<ListDisksCommand.ListDisksCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, ListDisksCommandSettings settings, CancellationToken cancellationToken)
    {
        string url;
        if (!string.IsNullOrWhiteSpace(settings.ResourceGroup))
            url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Compute/disks";
        else
            url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.Compute/disks";

        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, ListDisksCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListDisksCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Optional) Filter by resource group name.", required: false)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}
