using Topaz.ResourceManager;

namespace Topaz.Service.Disk.Models.Requests;

public sealed class UpdateDiskRequest
{
    public IDictionary<string, string>? Tags { get; set; }
    public DiskSkuRequest? Sku { get; set; }
    public UpdateDiskRequestProperties? Properties { get; set; }

    public sealed class DiskSkuRequest
    {
        public string? Name { get; set; }
    }

    public sealed class UpdateDiskRequestProperties
    {
        public long? DiskSizeGB { get; set; }
    }
}
