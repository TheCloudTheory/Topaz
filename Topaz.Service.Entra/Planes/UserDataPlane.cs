using System.Text.Json;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Models;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Planes;

internal sealed class UserDataPlane(EntraResourceProvider provider, ITopazLogger logger)
{
    public static UserDataPlane New(ITopazLogger logger) => new(new EntraResourceProvider(logger), logger);

    public DataPlaneOperationResult<User> CreateSuperadmin(CreateUserRequest request)
    {
        logger.LogDebug(nameof(UserDataPlane), nameof(Create), "Creating a superadmin user `{0}`.", request.UserPrincipalName);
        
        var entityPath = BuildLocalUserEntityPath(UserIdentifier.From(request.UserPrincipalName));

        if (File.Exists(entityPath))
        {
            logger.LogDebug(nameof(UserDataPlane), nameof(Create), "Superadmin user `{0}` already exists.", request.UserPrincipalName);
            return new DataPlaneOperationResult<User>(OperationResult.Success, null, null, null);;
        }

        var user = User.FromRequest(request, Guid.Empty);
        File.WriteAllText(entityPath, user.ToString());
        
        return new DataPlaneOperationResult<User>(OperationResult.Created, user, null, null);
    }
    
    public DataPlaneOperationResult<User> Create(CreateUserRequest request)
    {
        logger.LogDebug(nameof(UserDataPlane), nameof(Create), "Creating a user `{0}`.", request.UserPrincipalName);
        
        var entityPath = BuildLocalUserEntityPath(UserIdentifier.From(request.UserPrincipalName));

        if (File.Exists(entityPath))
        {
            return BadRequestOperationResult<User>.ForDuplicate("userPrincipalName");
        }

        var user = User.FromRequest(request);
        File.WriteAllText(entityPath, user.ToString());
        
        return new DataPlaneOperationResult<User>(OperationResult.Created, user, null, null);
    }

    private string BuildLocalUserEntityPath(UserIdentifier userIdentifier)
    {
        var path = provider.GetServiceInstanceUsersDataPath();
        var fileName = $"{userIdentifier}.json";
        var entityPath = Path.Combine(path, fileName);
        return entityPath;
    }

    public DataPlaneOperationResult<User> Get(UserIdentifier userIdentifier)
    {
        logger.LogDebug(nameof(UserDataPlane), nameof(Get), "Fetching a user `{0}`.", userIdentifier);

        User? user;
        var entityPath = BuildLocalUserEntityPath(userIdentifier);

        if (!File.Exists(entityPath))
        {
            logger.LogDebug(nameof(UserDataPlane), nameof(Get), "Didn't find a user `{0}` so will perform a full search.", userIdentifier);
            
            // User entities are created using `UserPrincipalName` we if the first 
            // search fails, we may perform a secondary, more expensive check by looking
            // at all entities and looking for the given ID
            var path = provider.GetServiceInstanceUsersDataPath();
            user = Directory.EnumerateFiles(path, "*.json")
                .Select(file => JsonSerializer.Deserialize<User>(File.ReadAllText(file), GlobalSettings.JsonOptions))
                .SingleOrDefault(u => u?.Id == userIdentifier.Value);

            return user == null
                ? BadRequestOperationResult<User>.ForNotFound(userIdentifier)
                : new DataPlaneOperationResult<User>(OperationResult.Success, user, null, null);
        }

        user = JsonSerializer.Deserialize<User>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions);
        return new DataPlaneOperationResult<User>(OperationResult.Success, user, null, null);
    }

    public DataPlaneOperationResult<User> Delete(UserIdentifier userIdentifier)
    {
        logger.LogDebug(nameof(UserDataPlane), nameof(Delete), "Deleting a user `{0}`.", userIdentifier);

        var existingUserOperation = Get(userIdentifier);
        if (existingUserOperation.Result == OperationResult.NotFound || existingUserOperation.Resource == null)
        {
            return BadRequestOperationResult<User>.ForNotFound(userIdentifier);
        }
        
        var entityPath = BuildLocalUserEntityPath(UserIdentifier.From(existingUserOperation.Resource.UserPrincipalName));
        File.Delete(entityPath);
        
        return new DataPlaneOperationResult<User>(OperationResult.Deleted, null, null, null);
    }
}