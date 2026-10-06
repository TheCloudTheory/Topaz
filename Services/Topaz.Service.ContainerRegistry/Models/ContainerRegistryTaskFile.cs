namespace Topaz.Service.ContainerRegistry.Models;

internal sealed class ContainerRegistryTaskFile
{
	public string? Version { get; init; }
	public int? StepTimeout { get; init; }
	public string? WorkingDirectory { get; init; }
	public List<string>? Env { get; init; }
	public List<ContainerRegistryTaskSecret>? Secrets { get; init; }
	public List<ContainerRegistryTaskNetwork>? Networks { get; init; }
	public List<ContainerRegistryTaskVolume>? Volumes { get; init; }
	public ContainerRegistryTaskAliases? Alias { get; init; }
	public List<ContainerRegistryTaskStep>? Steps { get; init; }
}

internal sealed class ContainerRegistryTaskStep
{
	public string? Build { get; init; }
	public List<string>? Push { get; init; }
	public string? Cmd { get; init; }
	public bool? Detach { get; init; }
	public bool? DisableWorkingDirectoryOverride { get; init; }
	public string? EntryPoint { get; init; }
	public List<string>? Env { get; init; }
	public List<string>? Expose { get; init; }
	public string? Id { get; init; }
	public bool? IgnoreErrors { get; init; }
	public string? Isolation { get; init; }
	public bool? Keep { get; init; }
	public ContainerRegistryTaskNetwork? Network { get; init; }
	public List<string>? Ports { get; init; }
	public bool? Pull { get; init; }
	public bool? Privileged { get; init; }
	public int? Repeat { get; init; }
	public int? Retries { get; init; }
	public int? RetryDelay { get; init; }
	public ContainerRegistryTaskSecret? Secret { get; init; }
	public int? StartDelay { get; init; }
	public int? Timeout { get; init; }
	public List<ContainerRegistryTaskVolumeMount>? VolumeMounts { get; init; }
	public List<string>? When { get; init; }
	public string? User { get; init; }
	public string? WorkingDirectory { get; init; }
}

internal class CustomAlias
{
}

internal sealed class ContainerRegistryTaskSecret
{
	public string? Id { get; init; }
	public string? Keyvault { get; init; }
	public string? ClientID { get; init; }
}

internal sealed class ContainerRegistryTaskNetwork
{
	public string? Name { get; init; }
	public string? Driver { get; init; }
	public bool? IPv6 { get; init; }
	public bool? SkipCreation { get; init; }
	public bool? IsDefault { get; init; }
}

internal sealed class ContainerRegistryTaskVolume
{
	public string? Name { get; init; }
	public Dictionary<string, string>? Secret { get; init; }
}

internal sealed class ContainerRegistryTaskVolumeMount
{
	public string? Name { get; init; }
	public string? MountPath { get; init; }
}

internal sealed class ContainerRegistryTaskAliases
{
	public List<string>? Src { get; init; }
	public Dictionary<string, string>? Values { get; init; }
}