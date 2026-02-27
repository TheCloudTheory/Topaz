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
        return new BadRequestOperationResult<TResource>(default,
            string.Format(NotFoundErrorMessage, userIdentifier.Value));
    }

    public static DataPlaneOperationResult<TResource> ForNotFound(ServicePrincipalIdentifier servicePrincipalIdentifier)
    {
        return new BadRequestOperationResult<TResource>(default,
            string.Format(NotFoundErrorMessage, servicePrincipalIdentifier.Value));
    }

    public static DataPlaneOperationResult<TResource> ForNotFound(ApplicationIdentifier applicationIdentifier)
    {
        return new BadRequestOperationResult<TResource>(default,
            string.Format(NotFoundErrorMessage, applicationIdentifier.Value));
    }
}

internal sealed class BadRequestOperationResult(string reason)
    : DataPlaneOperationResult(OperationResult.BadRequest, reason, "Request_BadRequest")
{
    private const string DuplicateErrorMessage =
        "Another object with the same value for property {0} already exists.";

    private const string NotFoundErrorMessage =
        "Resource '{0}' does not exist or one of its queried reference-property objects are not present.";

    public static BadRequestOperationResult ForDuplicate(string propertyName)
    {
        return new BadRequestOperationResult(string.Format(DuplicateErrorMessage, propertyName));
    }

    public static DataPlaneOperationResult ForNotFound(UserIdentifier userIdentifier)
    {
        return new BadRequestOperationResult(string.Format(NotFoundErrorMessage, userIdentifier.Value));
    }

    public static DataPlaneOperationResult ForNotFound(ServicePrincipalIdentifier servicePrincipalIdentifier)
    {
        return new BadRequestOperationResult(string.Format(NotFoundErrorMessage, servicePrincipalIdentifier.Value));
    }
    
    public static DataPlaneOperationResult ForNotFound(ApplicationIdentifier applicationIdentifier)
    {
        return new BadRequestOperationResult(string.Format(NotFoundErrorMessage, applicationIdentifier.Value));
    }
}