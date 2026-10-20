using JetBrains.Annotations;

namespace Topaz.Shared;

[UsedImplicitly]
public sealed class LicenseManager
{
    private static LicenseType GetLicenseTypeForLicenseKey(string? licenseKey)
    {
        return LicenseType.Community;
    }

    public static void GetLicenseSplashScreen(string? licenseKey)
    {
        var licenseType = GetLicenseTypeForLicenseKey(licenseKey);
        
        Console.WriteLine($"License: {licenseType}");
        Console.WriteLine("");
    }
}