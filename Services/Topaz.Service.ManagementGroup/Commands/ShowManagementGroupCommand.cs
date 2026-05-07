using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup.Commands;

[UsedImplicitly]
[CommandDefinition("management-group show", "management-group", "Shows details of a management group.")]
[CommandExample("Show a management group", "topaz management-group show --name \"my-mg\"")]
public sealed class ShowManagementGroupCommand(ITopazLogger logger)
    : Command<ShowManagementGroupCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        var controlPlane = ManagementGroupControlPlane.New(logger);
        var operation = controlPlane.Get(settings.Name!);

        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            Console.Error.WriteLine($"Management group '{settings.Name}' not found.");
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
