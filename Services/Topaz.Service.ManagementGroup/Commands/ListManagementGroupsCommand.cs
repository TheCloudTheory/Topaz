using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup.Commands;

[UsedImplicitly]
[CommandDefinition("management-group list", "management-group", "Lists all management groups.")]
[CommandExample("List management groups", "topaz management-group list")]
public sealed class ListManagementGroupsCommand(ITopazLogger logger)
    : Command<ListManagementGroupsCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        var controlPlane = ManagementGroupControlPlane.New(logger);
        var operation = controlPlane.List();

        AnsiConsole.WriteLine(JsonSerializer.Serialize(operation.Resource, GlobalSettings.JsonOptionsCli));
        return 0;
    }

    [UsedImplicitly]
    public sealed class Settings : CommandSettings { }
}
