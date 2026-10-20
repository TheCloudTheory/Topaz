using Topaz.Shared;

namespace Topaz.Service.Shared.Infrastructure;

public sealed class QuotaHandler
{
    private static readonly IReadOnlyDictionary<QuotaType, int> QuotaLimits = new Dictionary<QuotaType, int>
    {
        { QuotaType.Subscription, 1 },
        { QuotaType.ResourceGroup, 10 }
    };
    
    private bool IsQuotaApplicable(LicenseType licenseType)
    {
        switch(licenseType)
        {
            case LicenseType.Free:
            case LicenseType.Trial:
                return true;
            default:
                return false;
        }
    }
}