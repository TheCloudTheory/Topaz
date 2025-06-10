using Azure.ResourceManager;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.AspNetCore.Extensions;

public sealed class TopazEnvironmentBuilder(Guid defaultSubscriptionId)
{
    public readonly ArmClient ArmClient = new(new AzureLocalCredential(), defaultSubscriptionId.ToString(),
        TopazArmClientOptions.New);
}