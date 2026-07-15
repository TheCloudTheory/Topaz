namespace Topaz.Service.Shared;

public static class EndpointPermissions
{
    /// <summary>
    /// Indicates that endpoint has no specific permissions and its authorization is done using a dedicated component.
    /// Note that this doesn't imply an endpoint has "any permission" semantics.
    /// </summary>
    public static string[] None => [];
}