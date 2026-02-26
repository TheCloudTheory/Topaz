using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.Authorization.Domain;
using Topaz.Service.Authorization.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Authorization.Commands;

[UsedImplicitly]
[CommandDefinition(
    "role assignment create",
    "authorization",
    "Creates (or updates) an Azure RBAC role assignment for a principal at a given scope.")]
[CommandExample(
    "Assign Reader at subscription scope to a managed identity (or service principal)",
    "topaz role assignment create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n" +
    "    --name 11111111-2222-3333-4444-555555555555 \\\n" +
    "    --role-definition-id acdd72a7-3385-48ef-bd42-f606fba81ae7 \\\n" +
    "    --principal-id 66666666-7777-8888-9999-000000000000 \\\n" +
    "    --principal-type ServicePrincipal \\\n" +
    "    --scope /subscriptions/36a28ebb-9370-46d8-981c-84efe02048ae")]
[CommandExample(
    "Assign Key Vault Secrets User at Key Vault scope (data-plane secrets access)",
    "topaz role assignment create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n" +
    "    --name aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee \\\n" +
    "    --role-definition-id 4633458b-17de-408a-b874-0445c86b69e6 \\\n" +
    "    --principal-id 66666666-7777-8888-9999-000000000000 \\\n" +
    "    --principal-type ServicePrincipal \\\n" +
    "    --scope /subscriptions/36a28ebb-9370-46d8-981c-84efe02048ae/resourceGroups/rg-local/providers/Microsoft.KeyVault/vaults/mykv")]
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
    
    public override ValidationResult Validate(CommandContext context, CreateRoleAssignmentCommandSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Name))
            return ValidationResult.Error("Role assignment name can't be null.");

        if (!Guid.TryParse(settings.Name, out _))
            return ValidationResult.Error("Role assignment name must be a valid GUID.");

        if (string.IsNullOrWhiteSpace(settings.RoleDefinitionId))
            return ValidationResult.Error("Role definition ID can't be null.");
        
        if (string.IsNullOrWhiteSpace(settings.PrincipalId))
            return ValidationResult.Error("Principal ID can't be null.");

        if (!Guid.TryParse(settings.PrincipalId, out _))
            return ValidationResult.Error("Principal ID must be a valid GUID.");

        if (string.IsNullOrWhiteSpace(settings.PrincipalType))
            return ValidationResult.Error("Principal type can't be null.");

        if (string.IsNullOrWhiteSpace(settings.Scope))
            return ValidationResult.Error("Scope can't be null.");

        if (!settings.Scope.StartsWith("/", StringComparison.Ordinal))
            return ValidationResult.Error("Scope must be a resource ID starting with `/` (for example `/subscriptions/<subId>`).");

        if (string.IsNullOrWhiteSpace(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateRoleAssignmentCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) role assignment name (GUID). This becomes the roleAssignment resource name.")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) role definition ID. Example: /providers/Microsoft.Authorization/roleDefinitions/<roleGuid>.")]
        [CommandOption("-d|--role-definition-id")]
        public string? RoleDefinitionId { get; set; }

        [CommandOptionDefinition("(Required) principal (object) ID in Entra ID (GUID).")]
        [CommandOption("-p|--principal-id")]
        public string? PrincipalId { get; set; }

        [CommandOptionDefinition("(Required) principal type. Common value: ServicePrincipal.")]
        [CommandOption("-t|--principal-type")]
        public string? PrincipalType { get; set; }

        [CommandOptionDefinition("(Required) scope for the role assignment. Example: /subscriptions/<subId> or a resource ID.")]
        [CommandOption("--scope")]
        public string? Scope { get; set; }

        [CommandOptionDefinition("(Required) subscription ID (GUID).")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}