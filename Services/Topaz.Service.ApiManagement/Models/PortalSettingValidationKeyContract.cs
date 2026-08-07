using Topaz.Service.Shared;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class PortalSettingValidationKeyContract : TopazApiModel
{
    public string? ValidationKey { get; init; }

    public static PortalSettingValidationKeyContract From(PortalDelegationSettingsResource? existingResource)
    {
        return new PortalSettingValidationKeyContract
        {
            ValidationKey = existingResource?.Properties.ValidationKey
        };
    }
}