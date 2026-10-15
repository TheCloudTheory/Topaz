using System.Diagnostics;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Executors;

/// <summary>
/// Shells out to the host Docker daemon to execute a DockerBuildRequest ACR run.
/// </summary>
[UsedImplicitly]
internal class AcrDockerExecutor(ITopazLogger logger) : ExecutorBase(logger)
{
    private readonly ITopazLogger _logger = logger;
    public bool IsAvailable() => CheckAvailability();

    /// <summary>
    /// Runs <c>docker build</c> (and optionally <c>docker push</c>) for a DockerBuildRequest run.
    /// Appends stdout/stderr to <paramref name="logPath"/> line-by-line.
    /// Returns <c>true</c> on success, <c>false</c> on non-zero exit or exception.
    /// </summary>
    public async Task<bool> ExecuteAsync(
        string contextPath,
        string dockerFilePath,
        string imageName,
        bool isPushEnabled,
        string logPath,
        CancellationToken cancellationToken)
    {
        string? tempDir = null;

        try
        {
            string buildContext;
            if (contextPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                contextPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                tempDir = GenerateTempDir();
                AppendLogAsync(logPath, $"Cloning context from {contextPath}...");
                var cloneOk = await RunProcessAsync("git", $"clone {contextPath} \"{tempDir}\"", logPath, null, cancellationToken);
                if (!cloneOk) return false;
                buildContext = tempDir;
            }
            else
            {
                buildContext = contextPath;
            }

            var buildArgs = $"build -f {dockerFilePath} -t {imageName} {buildContext}";
            AppendLogAsync(logPath, $"Running: docker {buildArgs}");
            var buildOk = await RunProcessAsync("docker", buildArgs, logPath, null, cancellationToken);
            if (!buildOk) return false;

            if (isPushEnabled)
            {
                AppendLogAsync(logPath, $"Running: docker push \"{imageName}\"");
                var pushOk = await RunProcessAsync("docker", $"push \"{imageName}\"", logPath, null, cancellationToken);
                if (!pushOk) return false;
            }

            AppendLogAsync(logPath, "Run completed successfully.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(nameof(AcrDockerExecutor), nameof(ExecuteAsync), "Error running docker build");
            AppendLogAsync(logPath, $"Error: {ex.Message}");
            return false;
        }
        finally
        {
            if (tempDir != null && Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { /* best-effort cleanup */ }
            }
        }
    }
}
