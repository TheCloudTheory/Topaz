using JetBrains.Annotations;

namespace Topaz.Service.ApiManagement.Models.Requests;

[UsedImplicitly]
internal sealed class CreateOrUpdatePortalSignInSettingsRequest
{
    public PortalSignInSettingsResourceProperties? Properties { get; set; }
}