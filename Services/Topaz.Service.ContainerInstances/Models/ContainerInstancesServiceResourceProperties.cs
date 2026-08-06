using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Topaz.Service.ContainerInstances.Models.Requests;

namespace Topaz.Service.ContainerInstances.Models;

[UsedImplicitly]
internal sealed class ContainerInstancesServiceResourceProperties
{
    public Container[]? Containers { get; set; }
    public InitContainerDefinition[]? InitContainers { get; set; }
    public ImageRegistryCredential[]? ImageRegistryCredentials { get; set; }
    public IpAddress? IpAddress { get; set; }
    public string? OsType { get; set; }
    public string ProvisioningState { get; set; } = "Succeeded";
    public string? RestartPolicy { get; set; }
    public string? Sku { get; set; }
    public string? Priority { get; set; }
    public Volume[]? Volumes { get; set; }
    public ContainerGroupInstanceView? InstanceView { get; set; }
    public ContainerGroupDiagnostics? Diagnostics { get; set; }
    public ContainerGroupSubnetId[]? SubnetIds { get; set; }
    public DnsConfiguration? DnsConfig { get; set; }
    public EncryptionProperties? EncryptionProperties { get; set; }
    public DeploymentExtensionSpec[]? Extensions { get; set; }
    public ConfidentialComputeProperties? ConfidentialComputeProperties { get; set; }
    public ContainerGroupProfileReference? ContainerGroupProfile { get; set; }
    public StandbyPoolProfileDefinition? StandbyPoolProfile { get; set; }
    public IdentityAcls? IdentityAcls { get; set; }
    public SecretReference[]? SecretReferences { get; set; }
    public bool? IsCreatedFromStandbyPool { get; set; }

    public static ContainerInstancesServiceResourceProperties From(CreateOrUpdateContainerGroupRequest request)
    {
        return new ContainerInstancesServiceResourceProperties
        {
            Containers = request.Properties.Containers,
            InitContainers = request.Properties.InitContainers,
            ImageRegistryCredentials = request.Properties.ImageRegistryCredentials,
            IpAddress = request.Properties.IpAddress,
            OsType = request.Properties.OsType,
            RestartPolicy = request.Properties.RestartPolicy,
            Sku = request.Properties.Sku,
            Priority = request.Properties.Priority,
            Volumes = request.Properties.Volumes,
            InstanceView = request.Properties.InstanceView,
            Diagnostics = request.Properties.Diagnostics,
            SubnetIds = request.Properties.SubnetIds,
            DnsConfig = request.Properties.DnsConfig,
            StandbyPoolProfile = request.Properties.StandbyPoolProfile,
            ConfidentialComputeProperties = request.Properties.ConfidentialComputeProperties,
            IdentityAcls = request.Properties.IdentityAcls,
            EncryptionProperties = request.Properties.EncryptionProperties,
            Extensions = request.Properties.Extensions,
            ContainerGroupProfile = request.Properties.ContainerGroupProfile,
            SecretReferences = request.Properties.SecretReferences,
            IsCreatedFromStandbyPool = request.Properties.IsCreatedFromStandbyPool,
        };
    }

    public void UpdateFromRequest(CreateOrUpdateContainerGroupRequest request)
    {
        Containers = request.Properties.Containers ?? Containers;
        InitContainers = request.Properties.InitContainers ?? InitContainers;
        ImageRegistryCredentials = request.Properties.ImageRegistryCredentials ?? ImageRegistryCredentials;
        IpAddress = request.Properties.IpAddress ?? IpAddress;
        OsType = request.Properties.OsType ?? OsType;
        RestartPolicy = request.Properties.RestartPolicy ?? RestartPolicy;
        Sku = request.Properties.Sku ?? Sku;
        Priority = request.Properties.Priority ?? Priority;
        Volumes = request.Properties.Volumes ?? Volumes;
        InstanceView = request.Properties.InstanceView ?? InstanceView;
        Diagnostics = request.Properties.Diagnostics ?? Diagnostics;
        SubnetIds = request.Properties.SubnetIds ?? SubnetIds;
        DnsConfig = request.Properties.DnsConfig ?? DnsConfig;
        StandbyPoolProfile = request.Properties.StandbyPoolProfile ?? StandbyPoolProfile;
        ConfidentialComputeProperties = request.Properties.ConfidentialComputeProperties ?? ConfidentialComputeProperties;
        IdentityAcls = request.Properties.IdentityAcls ?? IdentityAcls;
        EncryptionProperties = request.Properties.EncryptionProperties ?? EncryptionProperties;
        Extensions = request.Properties.Extensions ?? Extensions;
        ContainerGroupProfile = request.Properties.ContainerGroupProfile ?? ContainerGroupProfile;
        SecretReferences = request.Properties.SecretReferences ?? SecretReferences;
        IsCreatedFromStandbyPool = request.Properties.IsCreatedFromStandbyPool ?? IsCreatedFromStandbyPool;
    }
}

internal sealed class Container
{
    public string? Name { get; set; }
    public ContainerProperties? Properties { get; set; }
}

internal sealed class ContainerProperties
{
    public string? Image { get; set; }
    public string[]? Command { get; set; }
    public ContainerPort[]? Ports { get; set; }
    public EnvironmentVariable[]? EnvironmentVariables { get; set; }
    public ResourceRequirements? Resources { get; set; }
    public VolumeMount[]? VolumeMounts { get; set; }
    public ContainerProbe? LivenessProbe { get; set; }
    public ContainerProbe? ReadinessProbe { get; set; }
    public SecurityContextDefinition? SecurityContext { get; set; }
    public ConfigMap? ConfigMap { get; set; }
    public ContainerInstanceView? InstanceView { get; set; }
}

internal sealed class ContainerPort
{
    public int Port { get; set; }
    public string? Protocol { get; set; }
}

internal sealed class EnvironmentVariable
{
    public string? Name { get; set; }
    public string? Value { get; set; }
    public string? SecureValue { get; set; }
    public string? SecureValueReference { get; set; }
}

internal sealed class ResourceRequirements
{
    public ResourceRequests? Requests { get; set; }
    public ResourceLimits? Limits { get; set; }
}

internal sealed class ResourceRequests
{
    public double Cpu { get; set; }
    public double MemoryInGB { get; set; }
    public GpuResource? Gpu { get; set; }
}

internal sealed class ResourceLimits
{
    public double? Cpu { get; set; }
    public double? MemoryInGB { get; set; }
    public GpuResource? Gpu { get; set; }
}

internal sealed class GpuResource
{
    public int Count { get; set; }
    public string? Sku { get; set; }
}

internal sealed class VolumeMount
{
    public string? Name { get; set; }
    public string? MountPath { get; set; }
    public bool? ReadOnly { get; set; }
}

internal sealed class ContainerProbe
{
    public ContainerExec? Exec { get; set; }
    public ContainerHttpGet? HttpGet { get; set; }
    public int? InitialDelaySeconds { get; set; }
    public int? PeriodSeconds { get; set; }
    public int? FailureThreshold { get; set; }
    public int? SuccessThreshold { get; set; }
    public int? TimeoutSeconds { get; set; }
}

internal sealed class ContainerExec
{
    public string[]? Command { get; set; }
}

internal sealed class ContainerHttpGet
{
    public string? Path { get; set; }
    public int Port { get; set; }
    public string? Scheme { get; set; }
    public HttpHeader[]? HttpHeaders { get; set; }
}

internal sealed class HttpHeader
{
    public string? Name { get; set; }
    public string? Value { get; set; }
}

internal sealed class SecurityContextDefinition
{
    public bool? Privileged { get; set; }
    public bool? AllowPrivilegeEscalation { get; set; }
    public int? RunAsUser { get; set; }
    public int? RunAsGroup { get; set; }
    public string? SeccompProfile { get; set; }
    public SecurityContextCapabilities? Capabilities { get; set; }
}

internal sealed class SecurityContextCapabilities
{
    public string[]? Add { get; set; }
    public string[]? Drop { get; set; }
}

internal sealed class ConfigMap
{
    public IDictionary<string, string>? KeyValuePairs { get; set; }
}

internal sealed class ContainerInstanceView
{
    public int? RestartCount { get; set; }
    public ContainerState? CurrentState { get; set; }
    public ContainerState? PreviousState { get; set; }
    public Event[]? Events { get; set; }
}

internal sealed class ContainerState
{
    public string? State { get; set; }
    public string? DetailStatus { get; set; }
    public int? ExitCode { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? FinishTime { get; set; }
}

internal sealed class Event
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public int? Count { get; set; }
    public DateTimeOffset? FirstTimestamp { get; set; }
    public DateTimeOffset? LastTimestamp { get; set; }
    public string? Message { get; set; }
}

internal sealed class InitContainerDefinition
{
    public string? Name { get; set; }
    public InitContainerProperties? Properties { get; set; }
}

internal sealed class InitContainerProperties
{
    public string? Image { get; set; }
    public string[]? Command { get; set; }
    public EnvironmentVariable[]? EnvironmentVariables { get; set; }
    public VolumeMount[]? VolumeMounts { get; set; }
    public SecurityContextDefinition? SecurityContext { get; set; }
    public InitContainerInstanceView? InstanceView { get; set; }
}

internal sealed class InitContainerInstanceView
{
    public int? RestartCount { get; set; }
    public ContainerState? CurrentState { get; set; }
    public ContainerState? PreviousState { get; set; }
    public Event[]? Events { get; set; }
}

internal sealed class ImageRegistryCredential
{
    public string? Server { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? PasswordReference { get; set; }
    public string? Identity { get; set; }
    public string? IdentityUrl { get; set; }
}

internal sealed class IpAddress
{
    public Port[]? Ports { get; set; }
    public string? Type { get; set; }
    public string? Ip { get; set; } = "127.0.0.1";
    public string? DnsNameLabel { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DnsNameLabelReusePolicy AutoGeneratedDomainNameLabelScope { get; set; } = DnsNameLabelReusePolicy.Unsecure;
    public string? Fqdn { get; set; }
}

internal enum DnsNameLabelReusePolicy
{
    Unsecure,
    TenantReuse,
    SubscriptionReuse,
    ResourceGroupReuse,
    Noreuse
}

internal sealed class Port
{
    [JsonPropertyName("port")]
    public int PortNumber { get; set; }
    public string? Protocol { get; set; }
}

internal sealed class Volume
{
    public string? Name { get; set; }
    public AzureFileVolume? AzureFile { get; set; }
    public object? EmptyDir { get; set; }
    public GitRepoVolume? GitRepo { get; set; }
    public IDictionary<string, string>? Secret { get; set; }
    public IDictionary<string, string>? SecretReference { get; set; }
}

internal sealed class AzureFileVolume
{
    public string? ShareName { get; set; }
    public string? StorageAccountName { get; set; }
    public string? StorageAccountKey { get; set; }
    public string? StorageAccountKeyReference { get; set; }
    public bool? ReadOnly { get; set; }
    public string? UserAssignedIdentityClientId { get; set; }
}

internal sealed class GitRepoVolume
{
    public string? Repository { get; set; }
    public string? Revision { get; set; }
    public string? Directory { get; set; }
}

internal sealed class ContainerGroupInstanceView
{
    public string? State { get; set; }
    public Event[]? Events { get; set; }
}

internal sealed class ContainerGroupDiagnostics
{
    public LogAnalytics? LogAnalytics { get; set; }
}

internal sealed class LogAnalytics
{
    public string? WorkspaceId { get; set; }
    public string? WorkspaceKey { get; set; }
    public string? WorkspaceResourceId { get; set; }
    public string? LogType { get; set; }
    public IDictionary<string, string>? Metadata { get; set; }
}

internal sealed class ContainerGroupSubnetId
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

internal sealed class DnsConfiguration
{
    public string[]? NameServers { get; set; }
    public string? SearchDomains { get; set; }
    public string? Options { get; set; }
}

internal sealed class EncryptionProperties
{
    public string? VaultBaseUrl { get; set; }
    public string? KeyName { get; set; }
    public string? KeyVersion { get; set; }
    public string? Identity { get; set; }
}

internal sealed class DeploymentExtensionSpec
{
    public string? Name { get; set; }
    public DeploymentExtensionSpecProperties? Properties { get; set; }
}

internal sealed class DeploymentExtensionSpecProperties
{
    public string? ExtensionType { get; set; }
    public string? Version { get; set; }
    public object? Settings { get; set; }
    public object? ProtectedSettings { get; set; }
}

internal sealed class ConfidentialComputeProperties
{
    public string? CcePolicy { get; set; }
}

internal sealed class ContainerGroupProfileReference
{
    public string? Id { get; set; }
    public int? Revision { get; set; }
}

internal sealed class StandbyPoolProfileDefinition
{
    public string? Id { get; set; }
    public bool? FailContainerGroupCreateOnReuseFailure { get; set; }
}

internal sealed class IdentityAcls
{
    public string? DefaultAccess { get; set; }
    public IdentityAccessControl[]? Acls { get; set; }
}

internal sealed class IdentityAccessControl
{
    public string? Identity { get; set; }
    public string? Access { get; set; }
}

internal sealed class SecretReference
{
    public string? Name { get; set; }
    public string? SecretReferenceUri { get; set; }
    public string? Identity { get; set; }
}