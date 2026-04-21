namespace Topaz.Service.ManagementGroup.Models.Requests;

internal sealed class CreateOrUpdateManagementGroupRequest
{
    public CreateManagementGroupProperties? Properties { get; set; }
}

internal sealed class CreateManagementGroupProperties
{
    public string? DisplayName { get; set; }

    public CreateManagementGroupDetails? Details { get; set; }
}

internal sealed class CreateManagementGroupDetails
{
    public CreateParentGroupInfo? Parent { get; set; }
}

internal sealed class CreateParentGroupInfo
{
    public string? Id { get; set; }
}
