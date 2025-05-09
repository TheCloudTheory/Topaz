using Azure.Local.Service.Shared;
using Azure.Local.Shared;

namespace Azure.Local.Service.KeyVault;

public sealed class KeyVaultService(ILogger logger) : IServiceDefinition
{
    public static string LocalDirectoryPath => ".azure-key-vault";
    private readonly ILogger logger = logger;

    public string Name => "Azure Key Vault";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new KeyVaultEndpoint(this.logger)
    ];
}
