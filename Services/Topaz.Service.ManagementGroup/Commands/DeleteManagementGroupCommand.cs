using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup.Commands;

[UsedImplicitly]
[CommandDefinition("management-group delete", "management-group", "Deletes a management group.")]
[CommandExample("Delete a management group", "topaz management-group delete --name \"my-mg\"")]
public sealed class DeleteManagementGroupCommand(ITopazLogger logger)
    : Command<DeleteManagementGroupCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        var controlPlane = ManagementGroupControlPlane.New(logger);
        var operation = controlPlane.Delete(settings.Name!);

        if (operation.Result is not OperationResult.Deleted)
        {
            Console.Error.WriteLine($"Failed: {operation.Reason}");
            return 1;
        }

        AnsiConsole.WriteLine("Deleted.");
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
