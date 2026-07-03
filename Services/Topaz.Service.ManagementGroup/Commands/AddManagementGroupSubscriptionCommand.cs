using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ManagementGroup.Commands;

[UsedImplicitly]
[CommandDefinition("management-group subscription add", "management-group",
    "Associates a subscription with a management group.")]
[CommandExample("Associate a subscription",
    "topaz management-group subscription add --group-id \"my-mg\" --subscription-id \"<guid>\"")]
public sealed class AddManagementGroupSubscriptionCommand(HttpClient httpClient)
    : TopazHttpCommand<AddManagementGroupSubscriptionCommand.Settings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/providers/Microsoft.Management/managementGroups/{settings.GroupId}/subscriptions/{settings.SubscriptionId}";
        var (success, body) = await PutAsync(url, new { });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.GroupId))
            return ValidationResult.Error("Management group ID (--group-id) is required.");
        if (string.IsNullOrWhiteSpace(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID (--subscription-id) is required.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class Settings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Management group ID.", required: true)]
        [CommandOption("-g|--group-id")]
        public string? GroupId { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID to associate.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
