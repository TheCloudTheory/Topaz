using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ManagementGroup.Commands;

[UsedImplicitly]
[CommandDefinition("management-group hierarchy-settings update", "management-group",
    "Updates the hierarchy settings for a management group.")]
[CommandExample("Enable authorization requirement",
    "topaz management-group hierarchy-settings update --name \"my-mg\" --require-authorization true")]
[CommandExample("Change the default management group",
    "topaz management-group hierarchy-settings update --name \"my-mg\" --default-management-group \"new-default-mg\"")]
public sealed class UpdateHierarchySettingsCommand(HttpClient httpClient)
    : TopazHttpCommand<UpdateHierarchySettingsCommand.Settings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var url = $"{ArmBaseUrl}/providers/Microsoft.Management/managementGroups/{settings.Name}/settings/default";
        var (success, body) = await PatchAsync(url, new { properties = new { requireAuthorizationForGroupCreation = settings.RequireAuthorization, defaultManagementGroup = settings.DefaultManagementGroup } });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Name))
            return ValidationResult.Error("Management group name (--name) is required.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class Settings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Management group ID / name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("Require authorization to create child management groups.")]
        [CommandOption("--require-authorization")]
        public bool? RequireAuthorization { get; set; }

        [CommandOptionDefinition("ID of the default management group for new subscriptions.")]
        [CommandOption("--default-management-group")]
        public string? DefaultManagementGroup { get; set; }
    }
}
