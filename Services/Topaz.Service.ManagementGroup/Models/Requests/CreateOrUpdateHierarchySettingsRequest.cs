namespace Topaz.Service.ManagementGroup.Models.Requests;

internal sealed class CreateOrUpdateHierarchySettingsRequest
{
    public CreateOrUpdateHierarchySettingsRequestProperties? Properties { get; set; }
}

internal sealed class CreateOrUpdateHierarchySettingsRequestProperties
{
    public bool? RequireAuthorizationForGroupCreation { get; set; }

    public string? DefaultManagementGroup { get; set; }
}
