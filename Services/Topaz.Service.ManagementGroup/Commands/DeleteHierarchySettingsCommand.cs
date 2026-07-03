using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ManagementGroup.Commands;

[UsedImplicitly]
[CommandDefinition("management-group hierarchy-settings delete", "management-group",
    "Deletes the hierarchy settings for a management group.")]
[CommandExample("Delete hierarchy settings", "topaz management-group hierarchy-settings delete --name \"my-mg\"")]
public sealed class DeleteHierarchySettingsCommand(HttpClient httpClient)
    : TopazHttpCommand<DeleteHierarchySettingsCommand.Settings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/providers/Microsoft.Management/managementGroups/{settings.Name}/settings/default";
        if (!await DeleteAsync(url)) return 1;
        AnsiConsole.WriteLine($"Hierarchy settings deleted.");
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
    }
}
