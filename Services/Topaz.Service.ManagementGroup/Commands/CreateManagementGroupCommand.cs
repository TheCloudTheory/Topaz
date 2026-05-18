using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ManagementGroup.Commands;

[UsedImplicitly]
[CommandDefinition("management-group create", "management-group", "Creates or updates a management group.")]
[CommandExample("Create a management group",
    "topaz management-group create --name \"my-mg\" --display-name \"My Management Group\"")]
[CommandExample("Create a management group under a parent",
    "topaz management-group create --name \"child-mg\" --display-name \"Child MG\" \\\n" +
    "    --parent-id \"/providers/Microsoft.Management/managementGroups/parent-mg\"")]
public sealed class CreateManagementGroupCommand(HttpClient httpClient)
    : TopazHttpCommand<CreateManagementGroupCommand.Settings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var url = $"{ArmBaseUrl}/providers/Microsoft.Management/managementGroups/{settings.Name}";
        var (success, body) = await PutAsync(url, new { properties = new { displayName = settings.DisplayName } });
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

        [CommandOptionDefinition("Display name for the management group.")]
        [CommandOption("-d|--display-name")]
        public string? DisplayName { get; set; }

        [CommandOptionDefinition("ARM ID or name of the parent management group.")]
        [CommandOption("-p|--parent-id")]
        public string? ParentId { get; set; }
    }
}
