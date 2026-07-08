namespace Topaz.Service.CosmosDb.Models.Requests;

public sealed class CreateOrUpdateDatabaseAccountRequest
{
    public static CreateOrUpdateDatabaseAccountRequest FromResource(DatabaseAccountResource account) =>
        new()
        {
            Location = account.Location,
            Tags = account.Tags,
            Kind = account.Properties.Kind,
            Properties = new CreateOrUpdateDatabaseAccountRequestProperties
            {
                ConsistencyPolicy = account.Properties.ConsistencyPolicy,
                Locations = account.Properties.Locations,
                DatabaseAccountOfferType = account.Properties.DatabaseAccountOfferType,
                IpRules = account.Properties.IpRules,
                IsVirtualNetworkFilterEnabled = account.Properties.IsVirtualNetworkFilterEnabled,
                EnableAutomaticFailover = account.Properties.EnableAutomaticFailover,
                Capabilities = account.Properties.Capabilities,
                PublicNetworkAccess = account.Properties.PublicNetworkAccess,
                EnableFreeTier = account.Properties.EnableFreeTier,
                EnableAnalyticalStorage = account.Properties.EnableAnalyticalStorage,
                ApiProperties = account.Properties.ApiProperties,
                DisableLocalAuth = account.Properties.DisableLocalAuth
            }
        };

    public string? Location { get; set; }
    public IDictionary<string, string>? Tags { get; set; }
    public string? Kind { get; set; }
    public CreateOrUpdateDatabaseAccountRequestProperties? Properties { get; set; }

    public sealed class CreateOrUpdateDatabaseAccountRequestProperties
    {
        public ConsistencyPolicySettings? ConsistencyPolicy { get; set; }
        public DatabaseAccountLocation[]? Locations { get; set; }
        public string? DatabaseAccountOfferType { get; set; }
        public IpAddressOrRange[]? IpRules { get; set; }
        public bool? IsVirtualNetworkFilterEnabled { get; set; }
        public bool? EnableAutomaticFailover { get; set; }
        public Capability[]? Capabilities { get; set; }
        public string? PublicNetworkAccess { get; set; }
        public bool? EnableFreeTier { get; set; }
        public bool? EnableAnalyticalStorage { get; set; }
        public ApiProperties? ApiProperties { get; set; }
        public bool? DisableLocalAuth { get; set; }
    }
}
