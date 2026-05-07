using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup.Commands;

[UsedImplicitly]
[CommandDefinition("management-group hierarchy-settings show", "management-group",
    "Shows the hierarchy settings for a management group.")]
[CommandExample("Show hierarchy settings", "topaz management-group hierarchy-settings show --name \"my-mg\"")]
public sealed class ShowHierarchySettingsCommand(ITopazLogger logger)
    : Command<ShowHierarchySettingsCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        var controlPlane = ManagementGroupControlPlane.New(logger);
        var operation = controlPlane.GetHierarchySettings(settings.Name!);

        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            Console.Error.WriteLine($"Hierarchy settings for management group '{settings.Name}' not found.");
            return 1;
        }

        AnsiConsole.WriteLine(operation.Resource.ToString());
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
