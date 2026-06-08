using Topaz.Service.Disk.Models.Requests;

namespace Topaz.Service.Disk.Models;

public sealed class DiskResourceProperties
{
    public long? DiskSizeGB { get; set; }

    public long DiskSizeBytes => (DiskSizeGB ?? 0) * 1024L * 1024L * 1024L;

    public long DiskIOPSReadWrite { get; set; }

    public long DiskMBpsReadWrite { get; set; }

    public string? OsType { get; set; }

    public string? HyperVGeneration { get; set; }

    public DiskCreationData? CreationData { get; set; }

    public string DiskState { get; set; } = "Unattached";

    public string ProvisioningState => "Succeeded";

    public DateTimeOffset TimeCreated { get; init; }

    public Guid UniqueId { get; init; }

    public static DiskResourceProperties FromRequest(CreateOrUpdateDiskRequest request)
    {
        var properties = request.Properties;

        return new DiskResourceProperties
        {
            DiskSizeGB = properties?.DiskSizeGB,
            DiskIOPSReadWrite = properties?.DiskIOPSReadWrite ?? 0,
            DiskMBpsReadWrite = properties?.DiskMBpsReadWrite ?? 0,
            OsType = properties?.OsType,
            HyperVGeneration = properties?.HyperVGeneration,
            CreationData = properties?.CreationData == null
                ? null
                : new DiskCreationData
                {
                    CreateOption = properties.CreationData.CreateOption ?? "Empty",
                    SourceResourceId = properties.CreationData.SourceResourceId,
                    ImageReference = properties.CreationData.ImageReference == null
                        ? null
                        : new DiskImageReference
                        {
                            Id = properties.CreationData.ImageReference.Id
                        }
                },
            DiskState = "Unattached",
            TimeCreated = DateTimeOffset.UtcNow,
            UniqueId = Guid.NewGuid()
        };
    }

    public static void UpdateFromRequest(DiskResourceProperties properties, CreateOrUpdateDiskRequest request)
    {
        var requestProperties = request.Properties;
        if (requestProperties == null)
            return;

        if (requestProperties.DiskSizeGB.HasValue)
            properties.DiskSizeGB = requestProperties.DiskSizeGB;

        if (requestProperties.DiskIOPSReadWrite != 0)
            properties.DiskIOPSReadWrite = requestProperties.DiskIOPSReadWrite;

        if (requestProperties.DiskMBpsReadWrite != 0)
            properties.DiskMBpsReadWrite = requestProperties.DiskMBpsReadWrite;

        if (requestProperties.OsType != null)
            properties.OsType = requestProperties.OsType;

        if (requestProperties.HyperVGeneration != null)
            properties.HyperVGeneration = requestProperties.HyperVGeneration;

        if (requestProperties.CreationData != null)
            properties.CreationData = new DiskCreationData
            {
                CreateOption = requestProperties.CreationData.CreateOption ?? "Empty",
                SourceResourceId = requestProperties.CreationData.SourceResourceId,
                ImageReference = requestProperties.CreationData.ImageReference == null
                    ? null
                    : new DiskImageReference
                    {
                        Id = requestProperties.CreationData.ImageReference.Id
                    }
            };
    }
}

public sealed class DiskCreationData
{
    public string CreateOption { get; set; } = "Empty";
    public string? SourceResourceId { get; set; }
    public DiskImageReference? ImageReference { get; set; }
}

public sealed class DiskImageReference
{
    public string? Id { get; set; }
}
