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
        { "azureDataLakeStoreFileSystem", "datalake.topaz.local.dev" },
        { "acrLoginServer", "cr.topaz.local.dev" },
        { "sqlServerHostname", "sql.topaz.local.dev" },
        { "azureDataLakeAnalyticsCatalogAndJob", "analytics.topaz.local.dev" },
        { "keyVaultDns", "vault.topaz.local.dev" },
        // Port 8890 is included so that giovanni v0.28.0's ParseAccountID (used by azurerm v4)
        // passes its strings.HasSuffix(uri.Host, domainSuffix) check.  uri.Host includes the
        // non-standard port, so the suffix must match the full host+port string.
        { "storage", "storage.topaz.local.dev:8890" },
        { "azureFrontDoorEndpointSuffix", "frontdoor.topaz.local.dev" },
        { "storageSyncEndpointSuffix", "storagesync.topaz.local.dev" },
        { "mhsmDns", "managedhsm.topaz.local.dev" },
        { "mysqlServerEndpoint", "mysql.topaz.local.dev" },
        { "postgresqlServerEndpoint", "postgres.topaz.local.dev" },
        { "mariadbServerEndpoint", "mariadb.topaz.local.dev" },
        { "synapseAnalytics", "synapse.topaz.local.dev" },
        { "attestationEndpoint", "attest.topaz.local.dev" }
    };

    public string Name => "public";

    public AuthenticationMetadata Authentication => new AuthenticationMetadata();

    internal class AuthenticationMetadata
    {
        public string LoginEndpoint => "https://topaz.local.dev:8899/";

        public string IdentityProvider => "AAD";

        public string Tenant => "common";
        
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