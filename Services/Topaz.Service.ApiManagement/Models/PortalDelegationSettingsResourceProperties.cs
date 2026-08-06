using Topaz.Service.ApiManagement.Models.Requests;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class PortalDelegationSettingsResourceProperties
{
    public SubscriptionsDelegationSettingsProperties? Subscriptions { get; set; }
    public string? Url { get; set; }
    public RegistrationDelegationSettingsProperties? UserRegistration { get; set; }
    public string? ValidationKey { get; set; }
    
    internal class SubscriptionsDelegationSettingsProperties
    {
        public bool Enabled { get; set; }
    }
    
    internal class RegistrationDelegationSettingsProperties
    {
        public bool Enabled { get; set; }
    }

    public static PortalDelegationSettingsResourceProperties From(CreateOrUpdatePortalDelegationSettingsRequest request)
    {
        return new PortalDelegationSettingsResourceProperties
        {
            Subscriptions = new SubscriptionsDelegationSettingsProperties
            {
                Enabled = request.Properties?.Subscriptions?.Enabled ?? false
            },
            Url = request.Properties?.Url,
            UserRegistration = new RegistrationDelegationSettingsProperties
            {
                Enabled = request.Properties?.UserRegistration?.Enabled ?? false
            },
            ValidationKey = request.Properties?.ValidationKey
        };
    }
}