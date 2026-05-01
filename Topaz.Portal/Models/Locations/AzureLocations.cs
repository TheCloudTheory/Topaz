namespace Topaz.Portal.Models.Locations;

public static class AzureLocations
{
    public static readonly IReadOnlyList<AzureLocation> CommonLocations =
    [
        // US Regions
        new("East US", "eastus"),
        new("East US 2", "eastus2"),
        new("East US STG", "eastusstg"),
        new("West US", "westus"),
        new("West US 2", "westus2"),
        new("West US 3", "westus3"),
        new("North Central US", "northcentralus"),
        new("South Central US", "southcentralus"),
        new("Central US", "centralus"),
        new("Central US EUAP", "centraluseuap"),
        new("West Central US", "westcentralus"),

        // Canada Regions
        new("Canada Central", "canadacentral"),
        new("Canada East", "canadaeast"),

        // Europe Regions
        new("North Europe", "northeurope"),
        new("West Europe", "westeurope"),
        new("France Central", "francecentral"),
        new("France South", "francesouth"),
        new("UK South", "uksouth"),
        new("UK West", "ukwest"),
        new("Germany West Central", "germanywestcentral"),
        new("Germany North", "germanynorth"),
        new("Switzerland North", "switzerlandnorth"),
        new("Switzerland West", "switzerlandwest"),
        new("Norway East", "norwayeast"),
        new("Sweden Central", "swedencentral"),
        new("Italy North", "italynorth"),
        new("Spain Central", "spaincentral"),
        new("Poland Central", "polandcentral"),

        // Asia Pacific Regions
        new("Southeast Asia", "southeastasia"),
        new("East Asia", "eastasia"),
        new("Australia East", "australiaeast"),
        new("Australia Southeast", "australiasoutheast"),
        new("Australia Central", "australiacentral"),
        new("Australia Central 2", "australiacentral2"),
        new("Central India", "centralindia"),
        new("South India", "southindia"),
        new("West India", "westindia"),
        new("Japan East", "japaneast"),
        new("Japan West", "japanwest"),
        new("Korea Central", "koreacentral"),
        new("Korea South", "koreasouth"),
        new("New Zealand North", "newzealandnorth"),
        new("Singapore", "singaporenorth"),

        // Middle East & Africa
        new("UAE North", "uaenorth"),
        new("UAE Central", "uaecentral"),
        new("South Africa North", "southafricanorth"),
        new("South Africa West", "southafricawest"),

        // Americas
        new("Brazil South", "brazilsouth"),
        new("Brazil Southeast", "brazilsoutheast"),
        new("Mexico Central", "mexicocentral"),
        new("Argentina Central", "argentinacentral"),
        new("Chile Central", "chilecentral"),

        // Government & Sovereign Clouds
        new("US Gov Virginia", "usgovvirginia"),
        new("US Gov Texas", "usgovtexas"),
        new("US Gov Arizona", "usgovarizona"),
        new("US Gov Iowa", "usgoviowa"),
        new("US DoD East", "usdodeast"),
        new("US DoD Central", "usdodcentral"),
        new("China East", "chinaeast"),
        new("China East 2", "chinaeast2"),
        new("China East 3", "chinaeast3"),
        new("China North", "chinanorth"),
        new("China North 2", "chinanorth2"),
        new("China North 3", "chinanorth3"),
    ];

    public class AzureLocation
    {
        public AzureLocation(string displayName, string code)
        {
            DisplayName = displayName;
            Code = code;
        }

        public string DisplayName { get; }
        public string Code { get; }
    }
}
