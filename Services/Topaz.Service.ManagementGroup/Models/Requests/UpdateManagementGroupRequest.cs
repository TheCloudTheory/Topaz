namespace Topaz.Service.ManagementGroup.Models.Requests;

internal sealed class UpdateManagementGroupRequest
{
    public UpdateManagementGroupProperties? Properties { get; set; }
}

internal sealed class UpdateManagementGroupProperties
{
    public string? DisplayName { get; set; }

    public string? ParentGroupId { get; set; }
}
