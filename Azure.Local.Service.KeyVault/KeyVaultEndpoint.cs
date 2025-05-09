using Azure.Local.Service.Shared;
using Microsoft.AspNetCore.Http;

namespace Azure.Local.Service.KeyVault;

public sealed class KeyVaultEndpoint : IEndpointDefinition
{
    public Protocol Protocol => Protocol.Https;

    public string[] Endpoints => [
        "subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{vaultName}"
    ];

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query)
    {
        throw new NotImplementedException();
    }
}
