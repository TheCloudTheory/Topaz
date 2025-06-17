using System.Text.Json;
using Topaz.Shared;

namespace Topaz.CloudEnvironment.Models.Responses;

internal sealed class GetMetadataEndpointResponse
{
    public IReadOnlyDictionary<string, string> Endpoints => new Dictionary<string, string>
    {
        { "resourceManager", "https://localhost:8899" },
        { "graphEndpoint", "https://localhost:8899" }
    };

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