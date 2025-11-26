using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses;

public sealed class ListDeletedResponse
{
    public DeletedKeyVaultResponse[] Value { get; set; } = [];

    public class DeletedKeyVaultResponse
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string Type => "Microsoft.KeyVault/deletedVaults";
        public DeletedKeyVaultProperties? Properties { get; set; }

        public class DeletedKeyVaultProperties
        {
            public string? VaultId { get; set; }
            public string? Location { get; set; }
            public IDictionary<string, string>? Tags { get; set; } =  new Dictionary<string, string>();
            public DateTimeOffset? DeletionDate  { get; set; }
            public DateTimeOffset? ScheduledPurgeDate { get; set; }
            public bool PurgeProtectionEnabled { get; set; }
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
        }
    }

    public string? NextLink { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}