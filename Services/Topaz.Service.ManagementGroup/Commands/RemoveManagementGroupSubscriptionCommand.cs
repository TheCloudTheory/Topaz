using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ManagementGroup.Commands;

[UsedImplicitly]
[CommandDefinition("management-group subscription remove", "management-group",
    "Disassociates a subscription from a management group.")]
[CommandExample("Remove a subscription from a management group",
    "topaz management-group subscription remove --group-id \"my-mg\" --subscription-id \"<guid>\"")]
public sealed class RemoveManagementGroupSubscriptionCommand(HttpClient httpClient)
    : TopazHttpCommand<RemoveManagementGroupSubscriptionCommand.Settings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var url = $"{ArmBaseUrl}/providers/Microsoft.Management/managementGroups/{settings.GroupId}/subscriptions/{settings.SubscriptionId}";
        if (!await DeleteAsync(url)) return 1;
        AnsiConsole.WriteLine($"Subscription removed from management group.");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
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

        [CommandOptionDefinition("(Required) Subscription ID to disassociate.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
