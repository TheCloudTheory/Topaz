using Microsoft.WindowsAzure.ResourceStack.Common.Collections;
using Newtonsoft.Json.Linq;

namespace Topaz.Service.ResourceManager.Deployment;

/// <summary>
/// Key constants and a typed dictionary for ARM template metadata injected at deployment time.
/// </summary>
public sealed class DeploymentMetadata : Dictionary<string, JToken>
{
    public const string SubscriptionKey = "subscription";
    public const string ResourceGroupKey = "resourceGroup";
}
