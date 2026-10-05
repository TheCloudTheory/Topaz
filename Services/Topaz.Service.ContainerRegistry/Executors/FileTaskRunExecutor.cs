using JetBrains.Annotations;
using Topaz.Service.ContainerRegistry.Models;
using Topaz.Service.ContainerRegistry.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;

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
                var cloneOk = await RunProcessAsync("git", $"clone {fileTaskStep.SourceLocation} \"{tempDir}\"", logPath, tempDir, cancellationToken);
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
                var result = await ParseAndRunFileTask(content, logPath, registryName, runId, tempDir, cancellationToken);
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
        string workingDirectory,
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

            var result = await ParseAndExecuteStep(step, logPath, registryName, runId, workingDirectory, cancellationToken);
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
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(step.Build))
        {
            await AppendLogAsync(logPath, $"Building image: {step.Build}");
            return await BuildImage(step.Build, registryName, logPath, runId, workingDirectory, cancellationToken);
        }
        
        if(step.Push != null && step.Push.All(p => !string.IsNullOrWhiteSpace(p)))
        {
            await AppendLogAsync(logPath, $"Pushing image: {string.Join(", ", step.Push)}");
            return await PushImage(step.Push, registryName, logPath, runId, workingDirectory, cancellationToken);
        }

        if (!string.IsNullOrEmpty(step.Cmd))
        {
            await AppendLogAsync(logPath, $"Running command: {step.Cmd}");
            return await RunCommand(step.Cmd, registryName, logPath, runId, workingDirectory, cancellationToken);
        }
        
        return false;
    }

    private static async Task<bool> RunCommand(string stepCmd, string registryName, string logPath, string runId, string workingDirectory, CancellationToken cancellationToken)
    {
        var compiledStep = CompileCommonPlaceholders(stepCmd, registryName, runId);
        var commandSegments = compiledStep.Split(' ');
        var image = commandSegments[0];

        // If CMD is `docker`, assume it's a Docker command and run it directly
        if (image == "docker")
        {
            return await RunProcessAsync("docker", string.Join(" ", commandSegments.Skip(1)), logPath, workingDirectory, cancellationToken);
        }
        
        return await RunProcessAsync("docker", $"run --rm {image} {string.Join(" ", commandSegments.Skip(1))}", logPath, workingDirectory, cancellationToken);
    }

    private static async Task<bool> PushImage(List<string> stepPush, string registryName, string logPath, string runId, string workingDirectory, CancellationToken cancellationToken)
    {
        foreach (var compiledStep in stepPush.Select(step => CompileCommonPlaceholders(step, registryName, runId)))
        {
            var result = await RunProcessAsync("docker", "push " + compiledStep, logPath, workingDirectory, cancellationToken);
            if (!result)
            {
                return false;
            }
        }

        return true;
    }

    private static async Task<bool> BuildImage(string stepBuild, string registryName, string logPath,
        string runId,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var compiledStep = CompileCommonPlaceholders(stepBuild, registryName, runId);
        var result = await RunProcessAsync("docker", "build " + compiledStep, logPath, workingDirectory, cancellationToken);
        
        return result;
    }

    private static string CompileCommonPlaceholders(string stepBuild, string registryName, string runId)
    {
        // The step definition may contain "$Registry" placeholder which
        // needs to be replaced with the actual registry name.
        var compiledStep = stepBuild.Replace("$Registry", GlobalSettings.GetContainerRegistryEndpoint(registryName));

        // We also need to replace "$RunId" placeholder with the actual run id.
        compiledStep = compiledStep.Replace("$ID", runId);
        
        compiledStep = compiledStep.Replace("$Date", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        return compiledStep;
    }
}