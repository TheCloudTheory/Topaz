using Azure.ResourceManager.Resources;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Service.Sql.Models.Requests;

public sealed class CreateOrUpdateSqlDatabaseRequest
{
    public string? Location { get; set; }
    public IDictionary<string, string>? Tags { get; set; }
    public ResourceSku? Sku { get; set; }
    public CreateOrUpdateSqlDatabaseRequestProperties? Properties { get; set; }

    public sealed class CreateOrUpdateSqlDatabaseRequestProperties
    {
        public string? Collation { get; set; }
        public long? MaxSizeBytes { get; set; }
        public bool? ZoneRedundant { get; set; }
        public string? LicenseType { get; set; }
        public string? RequestedBackupStorageRedundancy { get; set; }
    }

    public static CreateOrUpdateSqlDatabaseRequest From(GenericResourceData data)
    {
        return new CreateOrUpdateSqlDatabaseRequest
        {
            Location = data.Location,
            Tags = data.Tags,
            Sku = new ResourceSku
            {
                Capacity = data.Sku.Capacity,
                Name = data.Sku.Name,
                Tier = data.Sku.Tier,
                Family = data.Sku.Family
            },
            Properties =
                data.Properties.ToObjectFromJson<CreateOrUpdateSqlDatabaseRequestProperties>(GlobalSettings.JsonOptions)
        };
    }
}
