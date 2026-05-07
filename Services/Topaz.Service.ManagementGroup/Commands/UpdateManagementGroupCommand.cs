using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.ManagementGroup.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup.Commands;

[UsedImplicitly]
[CommandDefinition("management-group update", "management-group", "Updates a management group.")]
[CommandExample("Rename a management group",
    "topaz management-group update --name \"my-mg\" --display-name \"New Display Name\"")]
[CommandExample("Re-parent a management group",
    "topaz management-group update --name \"my-mg\" --parent-id \"parent-mg\"")]
public sealed class UpdateManagementGroupCommand(ITopazLogger logger)
    : Command<UpdateManagementGroupCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        var controlPlane = ManagementGroupControlPlane.New(logger);
        var operation = controlPlane.Update(settings.Name!, new UpdateManagementGroupRequest
        {
            Properties = new UpdateManagementGroupProperties
            {
                DisplayName = settings.DisplayName,
                ParentGroupId = settings.ParentId
            }
        });

        if (operation.Result is not OperationResult.Updated)
        {
            Console.Error.WriteLine($"Failed: {operation.Reason}");
            return 1;
        }

        AnsiConsole.WriteLine(operation.Resource!.ToString());
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

        [CommandOptionDefinition("New display name for the management group.")]
        [CommandOption("-d|--display-name")]
        public string? DisplayName { get; set; }

        [CommandOptionDefinition("ID of the new parent management group.")]
        [CommandOption("-p|--parent-id")]
        public string? ParentId { get; set; }
    }
}
