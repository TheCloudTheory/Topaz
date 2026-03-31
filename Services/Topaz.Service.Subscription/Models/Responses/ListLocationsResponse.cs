using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Subscription.Models.Responses;

internal sealed class ListLocationsResponse
{
    public ListLocationsResponse(string subscriptionId)
    {
        Value = AzureLocations.All(subscriptionId);
    }

    public LocationModel[] Value { get; init; }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

internal sealed class LocationModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string RegionalDisplayName { get; init; } = string.Empty;
    public LocationMetadataModel Metadata { get; init; } = new();
}

internal sealed class LocationMetadataModel
{
    public string RegionType { get; init; } = string.Empty;
    public string RegionCategory { get; init; } = string.Empty;
    public string GeographyGroup { get; init; } = string.Empty;
    public string Longitude { get; init; } = string.Empty;
    public string Latitude { get; init; } = string.Empty;
    public string PhysicalLocation { get; init; } = string.Empty;
}

internal static class AzureLocations
{
    public static LocationModel[] All(string subscriptionId) =>
    [
        Make(subscriptionId, "eastus",          "East US",              "(US) East US",              "US", "Physical", "Recommended", "-79.8164", "37.3719",  "Virginia"),
        Make(subscriptionId, "eastus2",         "East US 2",            "(US) East US 2",            "US", "Physical", "Recommended", "-78.3889", "36.6681",  "Virginia"),
        Make(subscriptionId, "westus",          "West US",              "(US) West US",              "US", "Physical", "Other",       "-122.417", "37.783",   "California"),
        Make(subscriptionId, "westus2",         "West US 2",            "(US) West US 2",            "US", "Physical", "Recommended", "-119.852", "47.233",   "Washington"),
        Make(subscriptionId, "westus3",         "West US 3",            "(US) West US 3",            "US", "Physical", "Recommended", "-112.074", "33.448",   "Phoenix"),
        Make(subscriptionId, "centralus",       "Central US",           "(US) Central US",           "US", "Physical", "Recommended", "-93.6208", "41.5908",  "Iowa"),
        Make(subscriptionId, "northcentralus",  "North Central US",     "(US) North Central US",     "US", "Physical", "Other",       "-87.6278", "41.8819",  "Illinois"),
        Make(subscriptionId, "southcentralus",  "South Central US",     "(US) South Central US",     "US", "Physical", "Recommended", "-98.5",    "29.4167",  "Texas"),
        Make(subscriptionId, "westcentralus",   "West Central US",      "(US) West Central US",      "US", "Physical", "Other",       "-110.234", "40.890",   "Wyoming"),
        Make(subscriptionId, "northeurope",     "North Europe",         "(Europe) North Europe",     "Europe", "Physical", "Recommended", "-8.472",  "53.3331", "Ireland"),
        Make(subscriptionId, "westeurope",      "West Europe",          "(Europe) West Europe",      "Europe", "Physical", "Recommended", "4.9",    "52.3667", "Netherlands"),
        Make(subscriptionId, "uksouth",         "UK South",             "(Europe) UK South",         "Europe", "Physical", "Recommended", "-0.799",  "50.941",  "London"),
        Make(subscriptionId, "ukwest",          "UK West",              "(Europe) UK West",          "Europe", "Physical", "Other",       "-3.084",  "53.427",  "Cardiff"),
        Make(subscriptionId, "francecentral",   "France Central",       "(Europe) France Central",   "Europe", "Physical", "Recommended", "2.3730",  "46.3772", "Paris"),
        Make(subscriptionId, "francesouth",     "France South",         "(Europe) France South",     "Europe", "Physical", "Other",       "2.1972",  "43.8345", "Marseille"),
        Make(subscriptionId, "germanywestcentral","Germany West Central","(Europe) Germany West Central","Europe","Physical","Recommended","8.682127","50.110924","Frankfurt"),
        Make(subscriptionId, "germanynorth",    "Germany North",        "(Europe) Germany North",    "Europe", "Physical", "Other",       "8.806422","53.073635","Berlin"),
        Make(subscriptionId, "switzerlandnorth","Switzerland North",    "(Europe) Switzerland North","Europe", "Physical", "Recommended", "8.564572","47.451542","Zurich"),
        Make(subscriptionId, "switzerlandwest", "Switzerland West",     "(Europe) Switzerland West", "Europe", "Physical", "Other",       "6.143158","46.204391","Geneva"),
        Make(subscriptionId, "norwayeast",      "Norway East",          "(Europe) Norway East",      "Europe", "Physical", "Recommended", "10.752245","59.913868","Oslo"),
        Make(subscriptionId, "norwaywest",      "Norway West",          "(Europe) Norway West",      "Europe", "Physical", "Other",       "5.733107","58.969975","Stavanger"),
        Make(subscriptionId, "swedencentral",   "Sweden Central",       "(Europe) Sweden Central",   "Europe", "Physical", "Recommended", "17.14127","60.67488", "Gävle"),
        Make(subscriptionId, "eastasia",        "East Asia",            "(Asia Pacific) East Asia",  "Asia Pacific", "Physical", "Other",       "114.188", "22.267",  "Hong Kong"),
        Make(subscriptionId, "southeastasia",   "Southeast Asia",       "(Asia Pacific) Southeast Asia","Asia Pacific","Physical","Recommended","103.833","1.283",  "Singapore"),
        Make(subscriptionId, "australiaeast",   "Australia East",       "(Asia Pacific) Australia East","Asia Pacific","Physical","Recommended","151.2094","-33.86",  "New South Wales"),
        Make(subscriptionId, "australiasoutheast","Australia Southeast","(Asia Pacific) Australia Southeast","Asia Pacific","Physical","Other","144.9631","-37.8136","Victoria"),
        Make(subscriptionId, "australiacentral","Australia Central",    "(Asia Pacific) Australia Central","Asia Pacific","Physical","Other","149.1244","-35.3075","Canberra"),
        Make(subscriptionId, "australiacentral2","Australia Central 2", "(Asia Pacific) Australia Central 2","Asia Pacific","Physical","Other","149.1244","-35.3075","Canberra"),
        Make(subscriptionId, "japaneast",       "Japan East",           "(Asia Pacific) Japan East", "Asia Pacific", "Physical", "Recommended", "139.77","35.68",   "Tokyo, Saitama"),
        Make(subscriptionId, "japanwest",       "Japan West",           "(Asia Pacific) Japan West", "Asia Pacific", "Physical", "Other",       "135.5022","34.6939","Osaka"),
        Make(subscriptionId, "koreacentral",    "Korea Central",        "(Asia Pacific) Korea Central","Asia Pacific","Physical","Recommended","126.978","37.5665","Seoul"),
        Make(subscriptionId, "koreasouth",      "Korea South",          "(Asia Pacific) Korea South","Asia Pacific","Physical","Other",       "129.0756","35.1796","Busan"),
        Make(subscriptionId, "centralindia",    "Central India",        "(Asia Pacific) Central India","Asia Pacific","Physical","Recommended","73.9197","18.5822","Pune"),
        Make(subscriptionId, "southindia",      "South India",          "(Asia Pacific) South India","Asia Pacific","Physical","Other",       "80.1636","12.9822","Chennai"),
        Make(subscriptionId, "westindia",       "West India",           "(Asia Pacific) West India", "Asia Pacific", "Physical", "Other",       "72.868","19.088",   "Mumbai"),
        Make(subscriptionId, "brazilsouth",     "Brazil South",         "(South America) Brazil South","South America","Physical","Recommended","-46.633","-23.55","São Paulo State"),
        Make(subscriptionId, "brazilsoutheast", "Brazil Southeast",     "(South America) Brazil Southeast","South America","Physical","Other","-43.2075","-22.90278","Rio"),
        Make(subscriptionId, "southafricanorth","South Africa North",   "(Africa) South Africa North","Africa","Physical","Recommended","28.21837","-25.731340","Johannesburg"),
        Make(subscriptionId, "southafricawest", "South Africa West",    "(Africa) South Africa West","Africa","Physical","Other","18.843266","-34.075691","Cape Town"),
        Make(subscriptionId, "uaenorth",        "UAE North",            "(Middle East) UAE North",   "Middle East", "Physical", "Recommended", "55.316666","25.266666","Dubai"),
        Make(subscriptionId, "uaecentral",      "UAE Central",          "(Middle East) UAE Central", "Middle East", "Physical", "Other",       "54.366669","24.466667","Abu Dhabi"),
        Make(subscriptionId, "canadacentral",   "Canada Central",       "(Canada) Canada Central",   "Canada", "Physical", "Recommended", "-79.383","43.653",  "Toronto"),
        Make(subscriptionId, "canadaeast",      "Canada East",          "(Canada) Canada East",      "Canada", "Physical", "Other",       "-71.217","46.817",  "Quebec"),
        Make(subscriptionId, "austriaeast",     "Austria East",         "(Europe) Austria East",     "Europe", "Physical", "Recommended", "16.372","48.208",   "Vienna"),
        Make(subscriptionId, "belgiumcentral",  "Belgium Central",      "(Europe) Belgium Central",  "Europe", "Physical", "Recommended", "4.3517","50.8503",  "Brussels"),
        Make(subscriptionId, "chilecentral",    "Chile Central",        "(South America) Chile Central","South America","Physical","Recommended","-70.6693","-33.4489","Santiago"),
        Make(subscriptionId, "denmarkeast",     "Denmark East",         "(Europe) Denmark East",     "Europe", "Physical", "Recommended", "12.5683","55.6761",  "Copenhagen"),
        Make(subscriptionId, "indonesiacentral","Indonesia Central",    "(Asia Pacific) Indonesia Central","Asia Pacific","Physical","Recommended","106.8456","-6.2088","Jakarta"),
        Make(subscriptionId, "israelcentral",   "Israel Central",       "(Middle East) Israel Central","Middle East","Physical","Recommended","34.8516","31.046",  "Israel"),
        Make(subscriptionId, "italynorth",      "Italy North",          "(Europe) Italy North",      "Europe", "Physical", "Recommended", "9.1859","45.4654",   "Milan"),
        Make(subscriptionId, "malaysiawest",    "Malaysia West",        "(Asia Pacific) Malaysia West","Asia Pacific","Physical","Recommended","101.6869","3.1412","Kuala Lumpur"),
        Make(subscriptionId, "mexicocentral",   "Mexico Central",       "(Mexico) Mexico Central",   "Mexico", "Physical", "Recommended", "-100.389","20.588",  "Querétaro State"),
        Make(subscriptionId, "newzealandnorth", "New Zealand North",    "(Asia Pacific) New Zealand North","Asia Pacific","Physical","Recommended","174.763","-36.848","Auckland"),
        Make(subscriptionId, "polandcentral",   "Poland Central",       "(Europe) Poland Central",   "Europe", "Physical", "Recommended", "21.0122","52.2297",  "Warsaw"),
        Make(subscriptionId, "qatarcentral",    "Qatar Central",        "(Middle East) Qatar Central","Middle East","Physical","Recommended","51.5310","25.2854", "Doha"),
        Make(subscriptionId, "spaincentral",    "Spain Central",        "(Europe) Spain Central",    "Europe", "Physical", "Recommended", "-3.7038","40.4168",  "Madrid"),
    ];

    private static LocationModel Make(
        string subscriptionId,
        string name,
        string displayName,
        string regionalDisplayName,
        string geographyGroup,
        string regionType,
        string regionCategory,
        string longitude,
        string latitude,
        string physicalLocation) =>
        new()
        {
            Id = $"/subscriptions/{subscriptionId}/locations/{name}",
            Name = name,
            DisplayName = displayName,
            RegionalDisplayName = regionalDisplayName,
            Metadata = new LocationMetadataModel
            {
                RegionType = regionType,
                RegionCategory = regionCategory,
                GeographyGroup = geographyGroup,
                Longitude = longitude,
                Latitude = latitude,
                PhysicalLocation = physicalLocation,
            }
        };
}
