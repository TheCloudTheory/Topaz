using Topaz.Service.Sql.Models.Requests;

namespace Topaz.Service.Sql.Models;

public sealed class SqlServerConnectionPolicySubresourceProperties
{
    public string ConnectionType { get; set; } = "Default";

    public static SqlServerConnectionPolicySubresourceProperties Default()
    {
        return new SqlServerConnectionPolicySubresourceProperties();
    }

    public static SqlServerConnectionPolicySubresourceProperties FromRequest(
        CreateOrUpdateSqlServerConnectionPolicyRequest request)
    {
        return new SqlServerConnectionPolicySubresourceProperties
        {
            ConnectionType = request.Properties?.ConnectionType ?? "Default"
        };
    }
}
