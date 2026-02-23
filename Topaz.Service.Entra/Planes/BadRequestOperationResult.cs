using Topaz.Service.Entra.Domain;
using Topaz.Service.Shared;

namespace Topaz.Service.Entra.Planes;

internal sealed class BadRequestOperationResult<TResource>(TResource? resource, string reason) 
    : DataPlaneOperationResult<TResource>(OperationResult.BadRequest, resource, reason, "Request_BadRequest")
{
    private const string DuplicateErrorMessage =
        "Another object with the same value for property {0} already exists.";
    private const string NotFoundErrorMessage =
        "Resource '{0}' does not exist or one of its queried reference-property objects are not present.";
    
    public static BadRequestOperationResult<TResource> ForDuplicate(string propertyName)
    {
        return new BadRequestOperationResult<TResource>(default, string.Format(DuplicateErrorMessage, propertyName));
    }

    public static DataPlaneOperationResult<TResource> ForNotFound(UserIdentifier userIdentifier)
    {
        return new BadRequestOperationResult<TResource>(default, string.Format(NotFoundErrorMessage, userIdentifier.Value));
    }
}