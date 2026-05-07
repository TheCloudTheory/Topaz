using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.ManagementGroup.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup.Commands;

[UsedImplicitly]
[CommandDefinition("management-group create", "management-group", "Creates or updates a management group.")]
[CommandExample("Create a management group",
    "topaz management-group create --name \"my-mg\" --display-name \"My Management Group\"")]
[CommandExample("Create a management group under a parent",
    "topaz management-group create --name \"child-mg\" --display-name \"Child MG\" \\\n" +
    "    --parent-id \"/providers/Microsoft.Management/managementGroups/parent-mg\"")]
public sealed class CreateManagementGroupCommand(ITopazLogger logger)
    : Command<CreateManagementGroupCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        var controlPlane = ManagementGroupControlPlane.New(logger);

        var parentArmId = string.IsNullOrWhiteSpace(settings.ParentId)
            ? null
            : settings.ParentId!.StartsWith('/')
                ? settings.ParentId
                : $"/providers/Microsoft.Management/managementGroups/{settings.ParentId}";

        var operation = controlPlane.CreateOrUpdate(settings.Name!, new CreateOrUpdateManagementGroupRequest
        {
            Properties = new CreateManagementGroupProperties
            {
                DisplayName = settings.DisplayName ?? settings.Name,
                Details = parentArmId == null ? null : new CreateManagementGroupDetails
                {
                    Parent = new CreateParentGroupInfo { Id = parentArmId }
                }
            }
        });

        if (operation.Result is not (OperationResult.Created or OperationResult.Updated))
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

        [CommandOptionDefinition("Display name for the management group.")]
        [CommandOption("-d|--display-name")]
        public string? DisplayName { get; set; }

        [CommandOptionDefinition("ARM ID or name of the parent management group.")]
        [CommandOption("-p|--parent-id")]
        public string? ParentId { get; set; }
    }
}
