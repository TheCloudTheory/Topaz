using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup.Commands;

[UsedImplicitly]
[CommandDefinition("management-group subscription show", "management-group",
    "Shows a subscription association under a management group.")]
[CommandExample("Show a subscription under a management group",
    "topaz management-group subscription show --group-id \"my-mg\" --subscription-id \"<guid>\"")]
public sealed class ShowManagementGroupSubscriptionCommand(ITopazLogger logger)
    : Command<ShowManagementGroupSubscriptionCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        var controlPlane = ManagementGroupControlPlane.New(logger);
        var operation = controlPlane.GetSubscriptionUnderManagementGroup(settings.GroupId!, settings.SubscriptionId!);

        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            Console.Error.WriteLine($"Failed: {operation.Reason}");
            return 1;
        }

        AnsiConsole.WriteLine(operation.Resource.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.GroupId))
            return ValidationResult.Error("Management group ID (--group-id) is required.");
        if (string.IsNullOrWhiteSpace(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID (--subscription-id) is required.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class Settings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Management group ID.", required: true)]
        [CommandOption("-g|--group-id")]
        public string? GroupId { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
