using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.ManagementGroup.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup.Commands;

[UsedImplicitly]
[CommandDefinition("management-group hierarchy-settings update", "management-group",
    "Updates the hierarchy settings for a management group.")]
[CommandExample("Enable authorization requirement",
    "topaz management-group hierarchy-settings update --name \"my-mg\" --require-authorization true")]
[CommandExample("Change the default management group",
    "topaz management-group hierarchy-settings update --name \"my-mg\" --default-management-group \"new-default-mg\"")]
public sealed class UpdateHierarchySettingsCommand(ITopazLogger logger)
    : Command<UpdateHierarchySettingsCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        var controlPlane = ManagementGroupControlPlane.New(logger);
        var operation = controlPlane.UpdateHierarchySettings(settings.Name!,
            new UpdateHierarchySettingsRequest
            {
                Properties = new UpdateHierarchySettingsRequestProperties
                {
                    RequireAuthorizationForGroupCreation = settings.RequireAuthorization,
                    DefaultManagementGroup = settings.DefaultManagementGroup
                }
            });

        if (operation.Result is not OperationResult.Updated || operation.Resource == null)
        {
            Console.Error.WriteLine($"Failed: {operation.Reason}");
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

        [CommandOptionDefinition("Require authorization to create child management groups.")]
        [CommandOption("--require-authorization")]
        public bool? RequireAuthorization { get; set; }

        [CommandOptionDefinition("ID of the default management group for new subscriptions.")]
        [CommandOption("--default-management-group")]
        public string? DefaultManagementGroup { get; set; }
    }
}
