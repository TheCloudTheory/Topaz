using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry;

internal sealed class ContainerRegistryResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<ContainerRegistryService>(logger)
{
    private static readonly string RunLogsDirectory =
        Path.Combine(GlobalSettings.MainEmulatorDirectory, ".acr-run-logs");

    /// <summary>Returns the path of the log file for an ACR run (created on demand).</summary>
    public string GetRunLogPath(string runId)
    {
        Directory.CreateDirectory(RunLogsDirectory);
        return Path.Combine(RunLogsDirectory, runId + ".log");
    }

    /// <summary>Returns the log content for a run, or null if no log exists yet.</summary>
    public string? ReadRunLog(string runId)
    {
        var path = Path.Combine(RunLogsDirectory, runId + ".log");
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }
}
