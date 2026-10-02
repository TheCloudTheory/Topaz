using JetBrains.Annotations;
using Topaz.Service.ContainerRegistry.Models;
using Topaz.Service.ContainerRegistry.Models.Requests;
using Topaz.Service.Shared;

namespace Topaz.Service.ContainerRegistry.Executors;

[UsedImplicitly]
internal sealed class FileTaskRunExecutor : ExecutorBase
{
    public static async Task<bool> ExecuteAsync(ScheduleTaskRunRequest fileTaskStep, string logPath,
        string registryName,
        string runId,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        string? tempDir = null;
        
        try
        {
            if (!string.IsNullOrWhiteSpace(fileTaskStep.SourceLocation) && fileTaskStep.SourceLocation.EndsWith(".git") && !string.IsNullOrWhiteSpace(fileTaskStep.TaskFilePath))
            {
                await AppendLogAsync(logPath, $"Cloning context from {fileTaskStep.SourceLocation}...");
             
                tempDir = GenerateTempDir();
                var cloneOk = await RunProcessAsync("git", $"clone {fileTaskStep.SourceLocation} \"{tempDir}\"", logPath, cancellationToken);
                if (!cloneOk)
                {
                    await AppendLogAsync(logPath, $"Error cloning context from {fileTaskStep.SourceLocation}");
                    return false;
                }
                
                var taskFilePath = Path.Combine(tempDir, fileTaskStep.TaskFilePath);
                if (!File.Exists(taskFilePath))
                {
                    await AppendLogAsync(logPath, $"Task file {taskFilePath} does not exist");
                    return false;
                }
                
                var content = await File.ReadAllTextAsync(taskFilePath, cancellationToken);
                var result = await ParseAndRunFileTask(content, logPath, registryName, runId, cancellationToken);
                if (!result)
                {
                    await AppendLogAsync(logPath, $"Error parsing and running file task.");
                    return false;
                }
                
                await AppendLogAsync(logPath, "Run completed successfully.");
            }

            return true;
        }
        catch (Exception ex)
        {
            await AppendLogAsync(logPath, $"Error: {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> ParseAndRunFileTask(string content, string logPath, string registryName,
        string runId,
        CancellationToken cancellationToken)
    {
        var task = YamlSerializerFacade.Deserialize<ContainerRegistryTaskFile>(content);
        if (task.Steps == null)
        {
            await AppendLogAsync(logPath, "No steps defined.");
            return false;
        }

        foreach (var step in task.Steps)
        {
            if(cancellationToken.IsCancellationRequested)
            {
                await AppendLogAsync(logPath, "Run cancelled.");
                return false;
            }

            var result = await ParseAndExecuteStep(step, logPath, registryName, runId, cancellationToken);
            if (!result)
            {
                return false;
            }
        }

        return true;
    }

    private static async Task<bool> ParseAndExecuteStep(ContainerRegistryTaskStep step, string logPath,
        string registryName,
        string runId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(step.Build))
        {
            await AppendLogAsync(logPath, $"Building image: {step.Build}");
            return await BuildImage(step.Build, registryName, logPath, runId, cancellationToken);
        }
        
        return false;
    }

    private static async Task<bool> BuildImage(string stepBuild, string registryName, string logPath,
        string runId,
        CancellationToken cancellationToken)
    {
        // The build step definition may contain "$Registry" placeholder which
        // needs to be replaced with the actual registry name.
        var compiledBuild = stepBuild.Replace("$Registry", registryName);
        
        // We also need to replace "$RunId" placeholder with the actual run id.
        compiledBuild = compiledBuild.Replace("$ID", runId);

        var result = await RunProcessAsync("docker", compiledBuild, logPath, cancellationToken);
        return result;
    }
}