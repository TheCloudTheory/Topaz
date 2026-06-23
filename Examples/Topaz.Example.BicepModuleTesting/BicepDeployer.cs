using System.Diagnostics;

namespace Topaz.Example.BicepModuleTesting;

public static class BicepDeployer
{
    private static readonly string ModulesDirectory =
        Path.Combine(AppContext.BaseDirectory, "modules");

    public static void Deploy(
        string resourceGroup,
        string templateFileName,
        string? parameters = null)
    {
        var templatePath = Path.Combine(ModulesDirectory, templateFileName);
        var args = $"deployment group create " +
                   $"--resource-group {resourceGroup} " +
                   $"--template-file \"{templatePath}\"" +
                   (parameters is not null ? $" --parameters {parameters}" : "");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "az",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"Bicep deployment failed ({templateFileName}): " +
                process.StandardError.ReadToEnd());
    }
}
