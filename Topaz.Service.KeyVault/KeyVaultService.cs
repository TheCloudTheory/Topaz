using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Endpoints;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.KeyVault;

public sealed class KeyVaultService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".azure-key-vault");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "keyvault";
    public string Name => "Azure Key Vault";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new KeyVaultEndpoint(logger),
        new KeyVaultServiceEndpoint(eventPipeline, logger)
    ];

    public void Bootstrap()
    {
    }
}
