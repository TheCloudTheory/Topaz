using System.Collections.ObjectModel;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.Shared.Infrastructure;

[UsedImplicitly]
public sealed class QuotaHandler
{
    private static readonly ReadOnlyDictionary<QuotaType, int> QuotaLimits = new(new Dictionary<QuotaType, int>
    {
        { QuotaType.Subscription, 1 },
        { QuotaType.ResourceGroup, 10 }
    });
    
    private static readonly Dictionary<string, QuotaType> ServiceToQuotaType = new()
    {
        { "subscription", QuotaType.Subscription },
        { "resourcegroup", QuotaType.ResourceGroup }
    };
    
    public static bool IsQuotaApplicable(string service)
    {
        var licenseType = LicenseManager.LoadedLicenseType;
        return licenseType switch
        {
            LicenseType.Free or LicenseType.Trial => ServiceToQuotaType.TryGetValue(service, out var quotaType) &&
                                                     QuotaLimits.ContainsKey(quotaType),
            _ => false
        };
    }

    public static int GetQuotaForService(string service)
    {
        var quotaType = ServiceToQuotaType.GetValueOrDefault(service);
        return QuotaLimits.GetValueOrDefault(quotaType, 0);
    }
}