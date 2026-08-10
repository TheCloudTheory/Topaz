using Azure.ResourceManager.Resources;
using Topaz.Shared;

namespace Topaz.Service.Sql.Models.Requests;

public sealed class CreateOrUpdateSqlServerRequest
{
    public string? Location { get; set; }
    public IDictionary<string, string>? Tags { get; set; }
    public CreateOrUpdateSqlServerRequestProperties? Properties { get; set; }

    public sealed class CreateOrUpdateSqlServerRequestProperties
    {
        public string? AdministratorLogin { get; set; }
        public string? AdministratorLoginPassword { get; set; }
        public string? Version { get; set; }
        public string? PublicNetworkAccess { get; set; }
    }

    public static CreateOrUpdateSqlServerRequest From(GenericResourceData data)
    {
        return new CreateOrUpdateSqlServerRequest
        {
            Location = data.Location,
            Tags = data.Tags,
            Properties =
                data.Properties.ToObjectFromJson<CreateOrUpdateSqlServerRequestProperties>(GlobalSettings.JsonOptions)
        };
    }
}
