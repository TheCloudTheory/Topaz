using Topaz.Service.Sql.Models.Requests;

namespace Topaz.Service.Sql.Models;

public sealed class SqlServerResourceProperties
{
    public string? AdministratorLogin { get; set; }
    public string? AdministratorLoginPassword { get; set; }
    public string? FullyQualifiedDomainName { get; set; }
    public string State { get; set; } = "Ready";
    public string Version { get; set; } = "12.0";
    public string PublicNetworkAccess { get; set; } = "Enabled";
    public string ProvisioningState => "Succeeded";

    public static SqlServerResourceProperties Default(string serverName) => new()
    {
        FullyQualifiedDomainName = $"{serverName}.database.topaz.local.dev"
    };

    public static SqlServerResourceProperties FromRequest(string serverName, CreateOrUpdateSqlServerRequest request)
    {
        return new SqlServerResourceProperties
        {
            AdministratorLogin = request.Properties?.AdministratorLogin,
            AdministratorLoginPassword = request.Properties?.AdministratorLoginPassword,
            FullyQualifiedDomainName = $"{serverName}.database.topaz.local.dev",
            Version = request.Properties?.Version ?? "12.0",
            PublicNetworkAccess = request.Properties?.PublicNetworkAccess ?? "Enabled"
        };
    }

    public static void UpdateFromRequest(SqlServerResourceProperties properties, CreateOrUpdateSqlServerRequest request)
    {
        if (request.Properties?.AdministratorLoginPassword != null)
            properties.AdministratorLoginPassword = request.Properties.AdministratorLoginPassword;

        if (request.Properties?.Version != null)
            properties.Version = request.Properties.Version;

        if (request.Properties?.PublicNetworkAccess != null)
            properties.PublicNetworkAccess = request.Properties.PublicNetworkAccess;
    }
}
