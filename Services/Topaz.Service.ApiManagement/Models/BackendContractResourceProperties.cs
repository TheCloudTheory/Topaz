using Topaz.Service.ApiManagement.Models.Requests;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class BackendContractResourceProperties
{
    public BackendCircuitBreaker? CircuitBreaker { get; set; }
    public BackendCredentialsContract? Credentials { get; set; }
    public string? Description { get; set; }
    public Pool? Pool { get; set; }
    public BackendProperties? Properties { get; set; }
    public string? Protocol { get; set; }
    public BackendProxyContract? Proxy { get; set; }
    public string? ResourceId { get; set; }
    public string? Title { get; set; }
    public BackendTlsProperties? Tls { get; set; }
    public string? Type { get; set; }
    public string? Url { get; set; }

    public static BackendContractResourceProperties From(CreateOrUpdateBackendRequest request)
    {
        return new BackendContractResourceProperties
        {
            CircuitBreaker = request.Properties?.CircuitBreaker,
            Credentials = request.Properties?.Credentials,
            Description = request.Properties?.Description,
            Pool = request.Properties?.Pool,
            Properties = request.Properties?.Properties,
            Protocol = request.Properties?.Protocol,
            Proxy = request.Properties?.Proxy,
            ResourceId = request.Properties?.ResourceId,
            Title = request.Properties?.Title,
            Tls = request.Properties?.Tls,
            Type = request.Properties?.Type,
            Url = request.Properties?.Url,
        };
    }
}

internal sealed class BackendCircuitBreaker
{
    public CircuitBreakerRule[]? Rules { get; set; }
}

internal sealed class CircuitBreakerRule
{
    public bool? AcceptRetryAfter { get; set; }
    public CircuitBreakerFailureCondition? FailureCondition { get; set; }
    public string? Name { get; set; }
    public string? TripDuration { get; set; }
}

internal sealed class CircuitBreakerFailureCondition
{
    public long? Count { get; set; }
    public string[]? ErrorReasons { get; set; }
    public string? Interval { get; set; }
    public long? Percentage { get; set; }
    public FailureStatusCodeRange[]? StatusCodeRanges { get; set; }
}

internal sealed class FailureStatusCodeRange
{
    public int? Max { get; set; }
    public int? Min { get; set; }
}

internal sealed class BackendCredentialsContract
{
    public BackendAuthorizationHeaderCredentials? Authorization { get; set; }
    public string[]? Certificate { get; set; }
    public string[]? CertificateIds { get; set; }
    public Dictionary<string, string[]>? Header { get; set; }
    public Dictionary<string, string[]>? Query { get; set; }
}

internal sealed class BackendAuthorizationHeaderCredentials
{
    public string? Parameter { get; set; }
    public string? Scheme { get; set; }
}

internal sealed class Pool
{
    public BackendPoolItem[]? Services { get; set; }
}

internal sealed class BackendPoolItem
{
    public string? Id { get; set; }
    public int? Priority { get; set; }
    public int? Weight { get; set; }
}

internal sealed class BackendProperties
{
    public BackendServiceFabricClusterProperties? ServiceFabricCluster { get; set; }
}

internal sealed class BackendServiceFabricClusterProperties
{
    public string? ClientCertificateId { get; set; }
    public string? ClientCertificatethumbprint { get; set; }
    public string[]? ManagementEndpoints { get; set; }
    public int? MaxPartitionResolutionRetries { get; set; }
    public string[]? ServerCertificateThumbprints { get; set; }
    public X509CertificateName[]? ServerX509Names { get; set; }
}

internal sealed class X509CertificateName
{
    public string? IssuerCertificateThumbprint { get; set; }
    public string? Name { get; set; }
}

internal sealed class BackendProxyContract
{
    public string? Password { get; set; }
    public string? Url { get; set; }
    public string? Username { get; set; }
}

internal sealed class BackendTlsProperties
{
    public bool? ValidateCertificateChain { get; set; }
    public bool? ValidateCertificateName { get; set; }
}