using Azure.ResourceManager;

namespace Topaz.ResourceManager;

public class TopazArmClientOptions
{
    /// <summary>
    /// Creates a new instance of `ArmClientOptions` which is aware of Topaz emulator environment. 
    /// </summary>
    public static ArmClientOptions New => new()
    {
        Environment = new ArmEnvironment(new Uri("https://topaz.local.dev:8899"), "https://topaz.local.dev:8899")
    };
}