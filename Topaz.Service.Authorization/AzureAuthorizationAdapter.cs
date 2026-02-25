namespace Topaz.Service.Authorization;

public sealed class AzureAuthorizationAdapter
{
    public bool IsAuthorized(string[] requiredPermissions, string token)
    {
        return true;
    }
}