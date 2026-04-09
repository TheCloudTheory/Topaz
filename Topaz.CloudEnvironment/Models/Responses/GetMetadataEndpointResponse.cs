using System.Text.Json;
using Topaz.Shared;

namespace Topaz.CloudEnvironment.Models.Responses;

internal sealed class GetMetadataEndpointResponse
{
    public string ResourceManager => "https://topaz.local.dev:8899";

    public string ResourceManagerEndpoint => "https://topaz.local.dev:8899";

    public string ActiveDirectory => "https://topaz.local.dev:8899";

    public string ActiveDirectoryEndpoint => "https://topaz.local.dev:8899";

    public string ActiveDirectoryResourceId => "https://topaz.local.dev:8899";

    public string ActiveDirectoryGraphResourceId => "https://topaz.local.dev:8899";

    public string MicrosoftGraphResourceId => "https://topaz.local.dev:8899/";

    public IReadOnlyDictionary<string, string> Endpoints => new Dictionary<string, string>
    {
        { "resourceManager", "https://topaz.local.dev:8899" },
        { "resourceManagerEndpoint", "https://topaz.local.dev:8899" },
        { "activeDirectory", "https://topaz.local.dev:8899" },
        { "activeDirectoryEndpoint", "https://topaz.local.dev:8899" },
        { "activeDirectoryResourceId", "https://topaz.local.dev:8899" },
        { "activeDirectoryGraphResourceId", "https://topaz.local.dev:8899" },
        { "microsoftGraphResourceId", "https://topaz.local.dev:8899/" }
    };
    
    public IReadOnlyDictionary<string, string> Suffixes => new Dictionary<string, string>
    {
        { "keyvaultDns", ".vault.topaz.local.dev" }
    };

    public string Name => "Topaz";

    public AuthenticationMetadata Authentication => new AuthenticationMetadata();

    internal class AuthenticationMetadata
    {
        public string LoginEndpoint => "https://topaz.local.dev:8899/";
        
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