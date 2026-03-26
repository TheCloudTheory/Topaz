using System.Collections.ObjectModel;
using Azure.Deployments.Core.Definitions.Extensibility;

namespace Topaz.Service.ResourceManager;

internal static class TemplateFunctionProviders
{
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, DeploymentExtensionConfigItem>> GetProviders()
    {
        return new ReadOnlyDictionary<string, IReadOnlyDictionary<string, DeploymentExtensionConfigItem>>(
            new Dictionary<string, IReadOnlyDictionary<string, DeploymentExtensionConfigItem>>());
    }
}
