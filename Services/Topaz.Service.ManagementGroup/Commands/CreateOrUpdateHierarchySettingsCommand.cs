using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ManagementGroup.Commands;

[UsedImplicitly]
[CommandDefinition("management-group hierarchy-settings create-or-update", "management-group",
    "Creates or updates the hierarchy settings for a management group.")]
[CommandExample("Create hierarchy settings",
    "topaz management-group hierarchy-settings create-or-update --name \"my-mg\" --require-authorization")]
[CommandExample("Set a default management group",
    "topaz management-group hierarchy-settings create-or-update --name \"my-mg\" --default-management-group \"default-mg\"")]
public sealed class CreateOrUpdateHierarchySettingsCommand(HttpClient httpClient)
    : TopazHttpCommand<CreateOrUpdateHierarchySettingsCommand.Settings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/providers/Microsoft.Management/managementGroups/{settings.Name}/settings/default";
        var (success, body) = await PutAsync(url, new { properties = new { requireAuthorizationForGroupCreation = settings.RequireAuthorization, defaultManagementGroup = settings.DefaultManagementGroup } });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, Settings settings)
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
