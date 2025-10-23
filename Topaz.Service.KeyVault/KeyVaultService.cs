using Topaz.Service.KeyVault.Endpoints;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.KeyVault;

public sealed class KeyVaultService(ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => ".azure-key-vault";
    public static IReadOnlyCollection<string>? Subresources => null;
    public string Name => "Azure Key Vault";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new KeyVaultEndpoint(logger),
        new KeyVaultServiceEndpoint(logger)
    ];
}
