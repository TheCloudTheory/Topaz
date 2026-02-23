using Topaz.Service.Entra.Models;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Planes;

internal sealed class UserDataPlane(EntraResourceProvider provider, ITopazLogger logger)
{
    public DataPlaneOperationResult<User> CreateUser(CreateUserRequest request)
    {
        logger.LogDebug(nameof(UserDataPlane), nameof(CreateUser), "Creating a user.");
        
        var path = provider.GetServiceInstanceUsersDataPath();
        var fileName = $"{request.UserPrincipalName}.json";
        var entityPath = Path.Combine(path, fileName);

        if (File.Exists(entityPath))
        {
            return BadRequestOperationResult<User>.ForDuplicate("userPrincipalName");
        }

        var user = User.FromRequest(request);
        File.WriteAllText(entityPath, user.ToString());
        
        return new DataPlaneOperationResult<User>(OperationResult.Created, user, null, null);
    }
}