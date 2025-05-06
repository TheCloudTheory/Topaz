using Azure.Local.Service.Shared;

namespace Azure.Local.Service.KeyVault;

public sealed class KeyVaultService : IServiceDefinition
{
    internal const string LocalDirectoryPath = ".azure-key-vault";

    public string Name => "Azure Key Vault";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => throw new NotImplementedException();
}
