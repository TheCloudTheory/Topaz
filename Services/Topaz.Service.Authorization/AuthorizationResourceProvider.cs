using System.Text.Json;
using Topaz.Service.Authorization.Domain;
using Topaz.Service.Authorization.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Authorization;

internal sealed class ResourceAuthorizationResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<ResourceAuthorizationService>(logger);

internal sealed class ResourceGroupAuthorizationResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<ResourceGroupAuthorizationService>(logger);

internal sealed class RoleAssignmentResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<RoleAssignmentService>(logger);

internal sealed class RoleDefinitionResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<RoleDefinitionService>(logger)
{
    private readonly ITopazLogger _logger = logger;

    public RoleDefinitionResource[] ListBuiltInRoles(SubscriptionIdentifier subscriptionIdentifier)
    {
        _logger.LogDebug(nameof(RoleDefinitionResourceProvider), nameof(ListBuiltInRoles),
            "List built-in roles for `{0}` subscription.", subscriptionIdentifier);

        var definitions = new List<RoleDefinitionResource>();
        var rawFiles = Directory.EnumerateFiles("Data", "*.json", SearchOption.AllDirectories);
        foreach (var file in rawFiles)
        {
            _logger.LogDebug(nameof(RoleDefinitionResourceProvider), nameof(ListBuiltInRoles),
                "Loading contents of a `{0}` file as role definition.", file);

            var content = File.ReadAllText(file);
            var fileModel = JsonSerializer.Deserialize<RoleDefinition>(content, GlobalSettings.JsonOptions);

            if (fileModel == null)
            {
                _logger.LogError($"Could not deserialize `{file}` file as `{nameof(RoleDefinition)}`.");
                continue;
            }

            try
            {
                var definition = fileModel.ToRoleDefinitionResource(subscriptionIdentifier);
                definitions.Add(definition);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Could not convert `{file}` to `{nameof(RoleDefinitionResource)}`: {ex.Message}");
            }
        }

        return definitions.ToArray();
    }

    public RoleDefinitionResource? GetBuiltInRoleById(RoleDefinitionIdentifier roleDefinitionIdentifier)
    {
        _logger.LogDebug(nameof(RoleDefinitionResourceProvider), nameof(GetBuiltInRoleById),
            "Looking for built-in role by id `{0}`.", roleDefinitionIdentifier);

        var rawFiles = Directory.EnumerateFiles("Data", "*.json", SearchOption.AllDirectories);
        return (from fileModel in rawFiles.Select(File.ReadAllText)
                .Select(content => JsonSerializer.Deserialize<RoleDefinition>(content, GlobalSettings.JsonOptions))
                .OfType<RoleDefinition>()
            where fileModel.Name == roleDefinitionIdentifier.Value ||
                  fileModel.Id?.Contains(roleDefinitionIdentifier.Value) == true
            let properties = new RoleDefinitionResourceProperties
            {
                RoleName = fileModel.RoleName,
                Description = fileModel.Description,
                Type = fileModel.RoleType,
                AssignableScopes = fileModel.AssignableScopes,
                Permissions = fileModel.Permissions?.Select(p => new RoleDefinition.Permission
                {
                    Actions = p.Actions, NotActions = p.NotActions, DataActions = p.DataActions,
                    NotDataActions = p.NotDataActions
                }).ToArray()
            }
            let roleId = fileModel.Id ?? $"/providers/Microsoft.Authorization/roleDefinitions/{fileModel.Name}"
            select new RoleDefinitionResource(roleId, fileModel.Name!, properties)).FirstOrDefault();
    }
}