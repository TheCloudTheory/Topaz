namespace Topaz.Service.ApiManagement.Models.Requests;

internal sealed class CreateOrUpdateTenantAccessRequest
{
    public TenantAccessResourceProperties? Properties { get; init; }
}