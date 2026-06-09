using Topaz.ResourceManager;
using Topaz.Service.Disk.Models;

namespace Topaz.Service.Disk.Models.Requests;

public sealed class CreateOrUpdateDiskRequest
{
    public static CreateOrUpdateDiskRequest FromResource(DiskResource disk)
    {
        return new CreateOrUpdateDiskRequest
        {
            Location = disk.Location,
            Tags = disk.Tags,
            Sku = disk.Sku == null
                ? null
                : new DiskSkuRequest { Name = disk.Sku.Name },
            Properties = new CreateOrUpdateDiskRequestProperties
            {
                DiskSizeGB = disk.Properties.DiskSizeGB,
                DiskIOPSReadWrite = disk.Properties.DiskIOPSReadWrite,
                DiskMBpsReadWrite = disk.Properties.DiskMBpsReadWrite,
                OsType = disk.Properties.OsType,
                HyperVGeneration = disk.Properties.HyperVGeneration,
                CreationData = disk.Properties.CreationData == null
                    ? null
                    : new CreateOrUpdateDiskRequestProperties.DiskCreationDataRequest
                    {
                        CreateOption = disk.Properties.CreationData.CreateOption,
                        SourceResourceId = disk.Properties.CreationData.SourceResourceId,
                        ImageReference = disk.Properties.CreationData.ImageReference == null
                            ? null
                            : new CreateOrUpdateDiskRequestProperties.DiskCreationDataRequest.DiskImageReferenceRequest
                            {
                                Id = disk.Properties.CreationData.ImageReference.Id
                            }
                    }
            }
        };
    }

    public string? Location { get; set; }
    public IDictionary<string, string>? Tags { get; set; }
    public DiskSkuRequest? Sku { get; set; }
    public CreateOrUpdateDiskRequestProperties? Properties { get; set; }

    public sealed class DiskSkuRequest
    {
        public string? Name { get; set; }
    }

    public sealed class CreateOrUpdateDiskRequestProperties
    {
        public long? DiskSizeGB { get; set; }
        public long DiskIOPSReadWrite { get; set; }
        public long DiskMBpsReadWrite { get; set; }
        public string? OsType { get; set; }
        public string? HyperVGeneration { get; set; }
        public string? PublicNetworkAccess { get; set; }
        public string? NetworkAccessPolicy { get; set; }
        public DiskCreationDataRequest? CreationData { get; set; }

        public sealed class DiskCreationDataRequest
        {
            public string? CreateOption { get; set; }
            public string? SourceResourceId { get; set; }
            public DiskImageReferenceRequest? ImageReference { get; set; }

            public sealed class DiskImageReferenceRequest
            {
                public string? Id { get; set; }
            }
        }
    }
}
