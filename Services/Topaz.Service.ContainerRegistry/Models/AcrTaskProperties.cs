using System.Text.Json.Serialization;

namespace Topaz.Service.ContainerRegistry.Models;

internal sealed class AcrTaskPlatformProperties
{
    public string? Os { get; init; }
    public string? Architecture { get; init; }
    public string? Variant { get; init; }
}

internal sealed class AcrTaskAgentProperties
{
    public int? Cpu { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AcrTaskDockerBuildStep), "Docker")]
[JsonDerivedType(typeof(AcrTaskEncodedTaskStep), "EncodedTask")]
[JsonDerivedType(typeof(AcrTaskFileTaskStep), "FileTask")]
internal abstract class AcrTaskStepProperties
{
    public List<AcrTaskBaseImageDependency>? BaseImageDependencies { get; init; }
    public string? ContextAccessToken { get; init; }
    public string? ContextPath { get; init; }
}

internal sealed class AcrTaskDockerBuildStep : AcrTaskStepProperties
{
    public List<AcrTaskArgument>? Arguments { get; init; }
    public string? DockerFilePath { get; init; }
    public List<string>? ImageNames { get; init; }
    public bool? IsPushEnabled { get; init; }
    public bool? NoCache { get; init; }
    public string? Target { get; init; }
}

internal sealed class AcrTaskEncodedTaskStep : AcrTaskStepProperties
{
    public string? EncodedTaskContent { get; init; }
    public string? EncodedValuesContent { get; init; }
    public List<AcrTaskSetValue>? Values { get; init; }
}

internal sealed class AcrTaskFileTaskStep : AcrTaskStepProperties
{
    public string? TaskFilePath { get; init; }
    public List<AcrTaskSetValue>? Values { get; init; }
    public string? ValuesFilePath { get; init; }
}

internal sealed class AcrTaskArgument
{
    public string? Name { get; init; }
    public string? Value { get; init; }
    public bool? IsSecret { get; init; }
}

internal sealed class AcrTaskSetValue
{
    public string? Name { get; init; }
    public string? Value { get; init; }
    public bool? IsSecret { get; init; }
}

internal sealed class AcrTaskBaseImageDependency
{
    public string? Digest { get; init; }
    public string? Registry { get; init; }
    public string? Repository { get; init; }
    public string? Tag { get; init; }
    public string? Type { get; init; }
}

internal sealed class AcrTaskTriggerProperties
{
    public List<AcrTaskTimerTrigger>? TimerTriggers { get; init; }
    public List<AcrTaskSourceTrigger>? SourceTriggers { get; init; }
    public AcrTaskBaseImageTrigger? BaseImageTrigger { get; init; }
}

internal sealed class AcrTaskTimerTrigger
{
    public string? Name { get; init; }
    public string? Schedule { get; init; }
    public string? Status { get; init; }
}

internal sealed class AcrTaskSourceTrigger
{
    public string? Name { get; init; }
    public string? Status { get; init; }
    public AcrTaskSourceProperties? SourceRepository { get; init; }
    public List<string>? SourceTriggerEvents { get; init; }
}

internal sealed class AcrTaskSourceProperties
{
    public string? SourceControlType { get; init; }
    public string? RepositoryUrl { get; init; }
    public string? Branch { get; init; }
    public AcrTaskAuthInfo? SourceControlAuthProperties { get; init; }
}

internal sealed class AcrTaskAuthInfo
{
    public int? ExpiresIn { get; init; }
    public string? RefreshToken { get; init; }
    public string? Scope { get; init; }
    public string? Token { get; init; }
    public string? TokenType { get; init; }
}

internal sealed class AcrTaskBaseImageTrigger
{
    public string? Name { get; init; }
    public string? Status { get; init; }
    public string? BaseImageTriggerType { get; init; }
}

internal sealed class AcrTaskCredentials
{
    public Dictionary<string, AcrTaskCustomRegistryCredentials?>? CustomRegistries { get; init; }
    public AcrTaskSourceRegistryCredentials? SourceRegistry { get; init; }
}

internal sealed class AcrTaskCustomRegistryCredentials
{
    public string? Identity { get; init; }
    public AcrTaskSecretObject? Password { get; init; }
    public AcrTaskSecretObject? UserName { get; init; }
}

internal sealed class AcrTaskSourceRegistryCredentials
{
    public string? LoginMode { get; init; }
}

internal sealed class AcrTaskSecretObject
{
    public string? Type { get; init; }
    public string? Value { get; init; }
}