namespace Topaz.Service.ManagementGroup.Models.Requests;

internal sealed class UpdateHierarchySettingsRequest
{
    public UpdateHierarchySettingsRequestProperties? Properties { get; set; }
}

internal sealed class UpdateHierarchySettingsRequestProperties
{
    public bool? RequireAuthorizationForGroupCreation { get; set; }

    public string? DefaultManagementGroup { get; set; }
}
