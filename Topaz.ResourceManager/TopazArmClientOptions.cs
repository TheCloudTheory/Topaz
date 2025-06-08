using Azure.ResourceManager;

namespace Topaz.ResourceManager;

public class TopazArmClientOptions
{
    /// <summary>
    /// Creates a new instance of `ArmClientOptions` which is aware of Topaz emulator environment. 
    /// </summary>
    public static ArmClientOptions New => new ArmClientOptions
    {
        Environment = new ArmEnvironment(new Uri("https://localhost:8899"), "https://localhost:8899")
    };
}