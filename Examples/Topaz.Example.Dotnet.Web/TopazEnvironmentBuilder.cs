using Azure.ResourceManager;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Example.Dotnet.Web;

internal sealed class TopazEnvironmentBuilder(Guid defaultSubscriptionId)
{
    public readonly ArmClient ArmClient = new(new AzureLocalCredential(), defaultSubscriptionId.ToString(),
        TopazArmClientOptions.New);
}