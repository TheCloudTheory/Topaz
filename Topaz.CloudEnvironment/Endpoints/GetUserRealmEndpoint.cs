using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.CloudEnvironment.Endpoints;

internal sealed class GetUserRealmEndpoint : IEndpointDefinition
{
    public string[] Endpoints => ["GET /common/userrealm/{username}"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort, GlobalSettings.HttpsPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var pathValue = context.Request.Path.Value ?? string.Empty;
        var username = pathValue.ExtractValueFromPath(3) ?? string.Empty;
        var domain = username.Contains('@', StringComparison.Ordinal)
            ? username.Split('@', 2)[1]
            : "topaz.local.dev";

        var payload = $$"""
            {
              "ver": "1.0",
              "account_type": "Managed",
              "domain_name": "{{domain}}",
              "cloud_instance_name": "topaz.local.dev",
              "cloud_audience_urn": "urn:federation:MicrosoftOnline",
              "federation_protocol": "None"
            }
            """;

        response.CreateJsonContentResponse(payload);
    }
}
