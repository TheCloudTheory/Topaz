using System.Text.Json;
using Topaz.Shared;

namespace Topaz.CloudEnvironment.Models.Responses;

internal sealed class GetMetadataEndpointResponse
{
    public IReadOnlyDictionary<string, string> Endpoints => new Dictionary<string, string>
    {
        { "resourceManager", "https://topaz.local.dev:8899" },
        { "microsoftGraphResourceId", "https://graph.microsoft.com/" }
    };
    
    public IReadOnlyDictionary<string, string> Suffixes => new Dictionary<string, string>
    {
        { "keyvaultDns", ".vault.topaz.local.dev" }
    };

    public string Name => "Topaz";

    public AuthenticationMetadata Authentication => new AuthenticationMetadata();

    internal class AuthenticationMetadata
    {
        public string LoginEndpoint => "https://login.microsoftonline.com/";
        
        public string[] Audiences => [
            "https://management.core.windows.net/",
            "https://management.azure.com/"
        ];
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}