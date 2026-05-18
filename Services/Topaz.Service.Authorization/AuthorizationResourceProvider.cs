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

/// <summary>
/// Stores and retrieves role assignments scoped to a management group.
/// Files are kept under {emulatorDir}/.management-group/{mgId}/.role-assignment/{name}.json
/// </summary>
internal sealed class ManagementGroupRoleAssignmentResourceProvider(ITopazLogger logger)
{
    private static string GetDir(string mgId) =>
        Path.Combine(GlobalSettings.MainEmulatorDirectory, ".management-group", mgId, ".role-assignment");

    private static string GetPath(string mgId, string name) =>
        Path.Combine(GetDir(mgId), $"{name}.json");

    public void CreateOrUpdate(string mgId, string name, RoleAssignmentResource resource)
    {
        var dir = GetDir(mgId);
        Directory.CreateDirectory(dir);
        File.WriteAllText(GetPath(mgId, name), JsonSerializer.Serialize(resource, GlobalSettings.JsonOptions));
        logger.LogDebug(nameof(ManagementGroupRoleAssignmentResourceProvider), nameof(CreateOrUpdate),
            "Saved MG role assignment '{0}' under management group '{1}'.", name, mgId);
    }

    public RoleAssignmentResource[] ListForPrincipal(string mgId, string principalId)
    {
        var dir = GetDir(mgId);
        if (!Directory.Exists(dir)) return [];

        return Directory.EnumerateFiles(dir, "*.json")
            .Select(f => JsonSerializer.Deserialize<RoleAssignmentResource>(File.ReadAllText(f), GlobalSettings.JsonOptions))
            .Where(r => r != null && r.Properties.PrincipalId == principalId)
            .ToArray()!;
    }
}

/// <summary>
/// Finds which management groups a given subscription belongs to by reading the
/// existing management-group directory structure (no separate index file needed).
/// </summary>
internal sealed class ManagementGroupSubscriptionIndexProvider(ITopazLogger logger)
{
    private static string BasePath =>
        Path.Combine(GlobalSettings.MainEmulatorDirectory, ".management-group");

    public IEnumerable<string> FindManagementGroupsForSubscription(string subscriptionId)
    {
        if (!Directory.Exists(BasePath)) yield break;

        foreach (var groupDir in Directory.EnumerateDirectories(BasePath))
        {
            var mgId = Path.GetFileName(groupDir);
            var subFile = Path.Combine(groupDir, "subscriptions", $"{subscriptionId}.json");
            if (File.Exists(subFile))
            {
                logger.LogDebug(nameof(ManagementGroupSubscriptionIndexProvider),
                    nameof(FindManagementGroupsForSubscription),
                    "Subscription '{0}' is a member of management group '{1}'.", subscriptionId, mgId);
                yield return mgId;
            }
        }
    }
}