using JetBrains.Annotations;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Models;

namespace Topaz.Service.ContainerRegistry.Models.Requests;

[UsedImplicitly]
internal sealed class ScheduleTaskRunRequest : TopazApiRequest, IValidatable
{
	public AcrTaskPlatformProperties? Platform { get; init; }
	public string? TaskFilePath { get; init; }
	public string Type { get; init; } = string.Empty;
	public AcrTaskAgentProperties? AgentConfiguration { get; init; }
	public AcrTaskCredentials? Credentials { get; init; }
	public bool? IsArchiveEnabled { get; init; }
	public string? SourceLocation { get; init; }
	public int? Timeout { get; init; }
	public List<AcrTaskSetValue>? Values { get; init; }
	public string? ValuesFilePath { get; init; }

	public (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class
	{
		if (Platform == null)
		{
			return (false, "Platform is required.");
		}

		if (string.IsNullOrWhiteSpace(TaskFilePath))
		{
			return (false, "TaskFilePath is required.");
		}

		return Timeout is < 300 or > 28800 ? (false, "Timeout must be between 300 and 28800 seconds.") : (true, null);
	}
}