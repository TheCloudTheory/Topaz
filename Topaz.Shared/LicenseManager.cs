using JetBrains.Annotations;

namespace Topaz.Shared;

[UsedImplicitly]
public sealed class LicenseManager
{
    public static LicenseType? LoadedLicenseType { get; private set; }
    
    public static void LoadLicense(string? licenseKey)
    {
        LoadedLicenseType = GetLicenseTypeForLicenseKey(licenseKey);
    }
    
    private static LicenseType GetLicenseTypeForLicenseKey(string? licenseKey)
    {
        return LicenseType.Community;
    }

    public static void GetLicenseSplashScreen()
    {
        var licenseType = LoadedLicenseType;
        
        Console.WriteLine($"License: {licenseType}");
        Console.WriteLine("");
    }
}