using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models.Responses;

internal sealed class OpenIdConfigurationResponse(string? tenantId = null)
{
    private const string BaseUrl = "https://topaz.local.dev:8899";

    // MSAL validates that the issuer from OIDC discovery contains the tenant ID that was requested.
    // Return a tenant-specific issuer when a tenant ID is provided; fall back to the /organizations
    // path for requests that don't specify a concrete tenant.
    [JsonPropertyName("issuer")]
    public string Issuer => tenantId is not null
        ? $"{BaseUrl}/{tenantId}/v2.0"
        : $"{BaseUrl}/organizations/v2.0";

    [JsonPropertyName("authorization_endpoint")]
    public string AuthorizationEndpoint => $"{BaseUrl}/organizations/oauth2/v2.0/authorize";

    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint => $"{BaseUrl}/organizations/oauth2/v2.0/token";

    [JsonPropertyName("jwks_uri")]
    public string JwksUri => $"{BaseUrl}/common/discovery/v2.0/keys";

    [JsonPropertyName("userinfo_endpoint")]
    public string UserinfoEndpoint => $"{BaseUrl}/oidc/userinfo";

    [JsonPropertyName("end_session_endpoint")]
    public string EndSessionEndpoint => $"{BaseUrl}/common/oauth2/v2.0/logout";

    [JsonPropertyName("device_authorization_endpoint")]
    public string DeviceAuthorizationEndpoint => $"{BaseUrl}/organizations/oauth2/v2.0/devicecode";

    [JsonPropertyName("response_types_supported")]
    public string[] ResponseTypesSupported => [
        "code",
        "code id_token",
        "id_token",
        "token id_token",
        "token"
    ];

    [JsonPropertyName("response_modes_supported")]
    public string[] ResponseModesSupported => [
        "query",
        "fragment",
        "form_post"
    ];

    [JsonPropertyName("grant_types_supported")]
    public string[] GrantTypesSupported => [
        "authorization_code",
        "implicit",
        "refresh_token",
        "client_credentials",
        "password"
    ];

    [JsonPropertyName("subject_types_supported")]
    public string[] SubjectTypesSupported => [
        "pairwise"
    ];

    [JsonPropertyName("id_token_signing_alg_values_supported")]
    public string[] IdTokenSigningAlgValuesSupported => [
        "RS256"
    ];

    [JsonPropertyName("token_endpoint_auth_methods_supported")]
    public string[] TokenEndpointAuthMethodsSupported => [
        "client_secret_post",
        "client_secret_basic",
        "private_key_jwt"
    ];

    [JsonPropertyName("scopes_supported")]
    public string[] ScopesSupported => [
        "openid",
        "profile",
        "email",
        "offline_access",
        "User.Read"
    ];

    [JsonPropertyName("claims_supported")]
    public string[] ClaimsSupported => [
        "aud",
        "iss",
        "iat",
        "nbf",
        "exp",
        "aio",
        "appid",
        "appidacr",
        "email",
        "family_name",
        "given_name",
        "ipaddr",
        "name",
        "oid",
        "scp",
        "sub",
        "tid",
        "unique_name",
        "upn",
        "ver"
    ];

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
