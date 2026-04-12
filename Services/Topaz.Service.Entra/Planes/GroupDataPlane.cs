using System.Text.Json;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Models;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;
using Directory = System.IO.Directory;

namespace Topaz.Service.Entra.Planes;

internal sealed class GroupDataPlane(EntraResourceProvider provider, ITopazLogger logger)
{
    public static GroupDataPlane New(ITopazLogger logger) => new(new EntraResourceProvider(logger), logger);

    public DataPlaneOperationResult<Group[]> ListGroups()
    {
        logger.LogDebug(nameof(GroupDataPlane), nameof(ListGroups), "Listing groups");

        var path = provider.GetServiceInstanceGroupsDataPath();
        var files = Directory.EnumerateFiles(path, "*.json");

        return new DataPlaneOperationResult<Group[]>(OperationResult.Success,
            files.Select(file =>
                    JsonSerializer.Deserialize<Group>(File.ReadAllText(file), GlobalSettings.JsonOptions)!)
                .ToArray(),
            null, null);
    }

    public DataPlaneOperationResult<Group> Create(CreateGroupRequest request)
    {
        logger.LogDebug(nameof(GroupDataPlane), nameof(Create), "Creating a group `{0}`.", request.DisplayName);

        var groupIdentifier = GroupIdentifier.From(Guid.NewGuid().ToString());
        var entityPath = BuildLocalGroupEntityPath(groupIdentifier);

        if (File.Exists(entityPath))
        {
            return BadRequestOperationResult<Group>.ForDuplicate("displayName");
        }

        var group = Group.FromRequest(request, Guid.Parse(groupIdentifier.Value));
        File.WriteAllText(entityPath, group.ToString());

        return new DataPlaneOperationResult<Group>(OperationResult.Created, group, null, null);
    }

    public DataPlaneOperationResult<Group> Get(GroupIdentifier groupIdentifier)
    {
        logger.LogDebug(nameof(GroupDataPlane), nameof(Get), "Fetching group `{0}`.", groupIdentifier);

        var path = provider.GetServiceInstanceGroupsDataPath();
        var safeName = PathGuard.SanitizeName(groupIdentifier.Value);
        var entityPath = Path.Combine(path, $"{safeName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, path);

        if (!File.Exists(entityPath))
        {
            return new DataPlaneOperationResult<Group>(OperationResult.NotFound, null, null, null);
        }

        var group = JsonSerializer.Deserialize<Group>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions);
        return new DataPlaneOperationResult<Group>(OperationResult.Success, group, null, null);
    }

    public DataPlaneOperationResult Update(GroupIdentifier groupIdentifier, UpdateGroupRequest request)
    {
        logger.LogDebug(nameof(GroupDataPlane), nameof(Update), "Updating group `{0}`.", groupIdentifier);

        var existing = Get(groupIdentifier);
        if (existing.Result == OperationResult.NotFound || existing.Resource == null)
        {
            return BadRequestOperationResult.ForNotFound(groupIdentifier);
        }

        existing.Resource.UpdateFromRequest(request);

        var entityPath = BuildLocalGroupEntityPath(groupIdentifier);
        File.WriteAllText(entityPath, existing.Resource.ToString());

        return new DataPlaneOperationResult(OperationResult.Updated, null, null);
    }

    public DataPlaneOperationResult Delete(GroupIdentifier groupIdentifier)
    {
        logger.LogDebug(nameof(GroupDataPlane), nameof(Delete), "Deleting group `{0}`.", groupIdentifier);

        var entityPath = BuildLocalGroupEntityPath(groupIdentifier);
        if (!File.Exists(entityPath))
        {
            return new DataPlaneOperationResult(OperationResult.NotFound, null, null);
        }

        File.Delete(entityPath);
        return new DataPlaneOperationResult(OperationResult.Deleted, null, null);
    }

    private string BuildLocalGroupEntityPath(GroupIdentifier groupIdentifier)
    {
        var path = provider.GetServiceInstanceGroupsDataPath();
        var safeName = PathGuard.SanitizeName(groupIdentifier.Value);
        var fileName = $"{safeName}.json";
        var entityPath = Path.Combine(path, fileName);
        PathGuard.EnsureWithinDirectory(entityPath, path);
        return entityPath;
    }
}
