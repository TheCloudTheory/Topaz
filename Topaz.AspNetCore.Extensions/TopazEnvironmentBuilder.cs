using Azure.ResourceManager;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.AspNetCore.Extensions;

public sealed class TopazEnvironmentBuilder(Guid defaultSubscriptionId, string objectId)
{
    public readonly ArmClient ArmClient = new(new AzureLocalCredential(objectId), defaultSubscriptionId.ToString(),
        TopazArmClientOptions.New);
}