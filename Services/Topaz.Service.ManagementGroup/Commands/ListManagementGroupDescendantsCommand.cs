using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup.Commands;

[UsedImplicitly]
[CommandDefinition("management-group descendants list", "management-group",
    "Lists all descendant management groups and subscriptions under a management group.")]
[CommandExample("List descendants of a management group",
    "topaz management-group descendants list --name \"my-mg\"")]
public sealed class ListManagementGroupDescendantsCommand(ITopazLogger logger)
    : Command<ListManagementGroupDescendantsCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        var controlPlane = ManagementGroupControlPlane.New(logger);
        var operation = controlPlane.GetDescendants(settings.Name!);

        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            Console.Error.WriteLine($"Management group '{settings.Name}' not found.");
            return 1;
        }

        AnsiConsole.WriteLine(JsonSerializer.Serialize(operation.Resource, GlobalSettings.JsonOptionsCli));
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
    }
}
