using JetBrains.Annotations;
using Spectre.Console.Cli;
using Topaz.EventPipeline;
using Topaz.Service.Authorization.Domain;
using Topaz.Service.Authorization.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Authorization.Commands;

[UsedImplicitly]
public sealed class CreateRoleAssignmentCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<CreateRoleAssignmentCommand.CreateRoleAssignmentCommandSettings>
{
    public override int Execute(CommandContext context, CreateRoleAssignmentCommandSettings settings)
    {
        logger.LogDebug(nameof(CreateRoleAssignmentCommand), nameof(Execute), "Creating a role assignment...");
        
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var roleAssignmentName = RoleAssignmentName.From(settings.Name);
        var controlPlane = AuthorizationControlPlane.New(eventPipeline, logger);
        var operation = controlPlane.CreateRoleAssignment(subscriptionIdentifier, roleAssignmentName,
            new CreateOrUpdateRoleAssignmentRequest
            {
                Properties = new RoleAssignmentProperties
                {
                    PrincipalId = settings.PrincipalId,
                    PrincipalType = settings.PrincipalType,
                    RoleDefinitionId = settings.RoleDefinitionId,
                    Scope = settings.Scope
                }
            });

        if (operation.Result != OperationResult.Created)
        {
            logger.LogError(nameof(CreateRoleAssignmentCommand), nameof(Execute),
                $"Failed to create role assignment: {operation.Reason}");
            return 1;
        }
        
        logger.LogInformation(operation.Result.ToString());
        return 0;
    }

    [UsedImplicitly]
    public sealed class CreateRoleAssignmentCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")] public string? Name { get; set; }

        [CommandOption("-d|--role-definition-id")]
        public string? RoleDefinitionId { get; set; }

        [CommandOption("-p|--principal-id")] public string? PrincipalId { get; set; }

        [CommandOption("-t|--principal-type")] public string? PrincipalType { get; set; }

        [CommandOption("--scope")] public string? Scope { get; set; }

        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}