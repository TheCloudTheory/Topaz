namespace Topaz.Service.Sql.Models.Requests;

public sealed class CreateOrUpdateSqlServerConnectionPolicyRequest
{
    public CreateOrUpdateSqlServerConnectionPolicyRequestProperties? Properties { get; set; }

    public sealed class CreateOrUpdateSqlServerConnectionPolicyRequestProperties
    {
        public string? ConnectionType { get; set; }
    }
}
