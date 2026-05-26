using Topaz.Service.Sql.Models.Requests;

namespace Topaz.Service.Sql.Models;

public sealed class SqlDatabaseResourceProperties
{
    public string Collation { get; set; } = "SQL_Latin1_General_CP1_CI_AS";
    public string Status { get; set; } = "Online";
    public string DatabaseId { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;
    public long MaxSizeBytes { get; set; } = 2147483648;
    public bool ZoneRedundant { get; set; } = false;
    public string? LicenseType { get; set; }
    public string RequestedBackupStorageRedundancy { get; set; } = "Geo";
    public string ProvisioningState => "Succeeded";
    public string CurrentSkuName { get; set; } = "Basic";

    public static SqlDatabaseResourceProperties FromRequest(CreateOrUpdateSqlDatabaseRequest request) =>
        new()
        {
            Collation = request.Properties?.Collation ?? "SQL_Latin1_General_CP1_CI_AS",
            MaxSizeBytes = request.Properties?.MaxSizeBytes ?? 2147483648,
            ZoneRedundant = request.Properties?.ZoneRedundant ?? false,
            LicenseType = request.Properties?.LicenseType,
            RequestedBackupStorageRedundancy = request.Properties?.RequestedBackupStorageRedundancy ?? "Geo",
            CurrentSkuName = request.Sku?.Name ?? "Basic"
        };

    public static void UpdateFromRequest(SqlDatabaseResourceProperties properties,
        CreateOrUpdateSqlDatabaseRequest request)
    {
        if (request.Properties?.Collation != null)
            properties.Collation = request.Properties.Collation;

        if (request.Properties?.MaxSizeBytes != null)
            properties.MaxSizeBytes = request.Properties.MaxSizeBytes.Value;

        if (request.Properties?.ZoneRedundant != null)
            properties.ZoneRedundant = request.Properties.ZoneRedundant.Value;

        if (request.Properties?.LicenseType != null)
            properties.LicenseType = request.Properties.LicenseType;

        if (request.Properties?.RequestedBackupStorageRedundancy != null)
            properties.RequestedBackupStorageRedundancy = request.Properties.RequestedBackupStorageRedundancy;

        if (request.Sku?.Name != null)
            properties.CurrentSkuName = request.Sku.Name;
    }
}
