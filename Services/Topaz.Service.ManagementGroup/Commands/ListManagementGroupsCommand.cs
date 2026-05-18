using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ManagementGroup.Commands;

[UsedImplicitly]
[CommandDefinition("management-group list", "management-group", "Lists all management groups.")]
[CommandExample("List management groups", "topaz management-group list")]
public sealed class ListManagementGroupsCommand(HttpClient httpClient)
    : TopazHttpCommand<ListManagementGroupsCommand.Settings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var url = $"{ArmBaseUrl}/providers/Microsoft.Management/managementGroups";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    [UsedImplicitly]
    public sealed class Settings : CommandSettings { }
}
