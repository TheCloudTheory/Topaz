namespace Topaz.Service.Shared;

public enum ProvisioningState
{
    Creating,
    Updating,
    Deleting,
    Succeeded,
    Canceled,
    Failed,
    Deleted,
    DeleteFailed,
    CreateFailed,
    UpdatedFailed
}