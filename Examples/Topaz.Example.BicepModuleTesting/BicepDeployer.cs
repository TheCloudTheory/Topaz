using System.Diagnostics;

namespace Topaz.Example.BicepModuleTesting;

public static class BicepDeployer
{
    private static readonly string ModulesDirectory =
        Path.Combine(AppContext.BaseDirectory, "modules");

    public static void Login()
    {
        RunAz("login --username topazadmin@topaz.local.dev --password admin");
    }

    public static void Deploy(
        string resourceGroup,
        string templateFileName,
        string? parameters = null)
    {
        var templatePath = Path.Combine(ModulesDirectory, templateFileName);
        var args = $"deployment group create " +
                   "--only-show-errors " +
                   $"--resource-group {resourceGroup} " +
                   $"--template-file \"{templatePath}\"" +
                   (parameters is not null ? $" --parameters {parameters}" : "");

        RunAz(args, errorPrefix: $"Bicep deployment failed ({templateFileName})");
    }

    private static void RunAz(string args, string? errorPrefix = null)
    {
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

        process.StartInfo.Environment["AZURE_CORE_INSTANCE_DISCOVERY"] = "false";
        process.StartInfo.Environment["HTTPS_PROXY"] = "http://topaz.local.dev:44380";

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"{errorPrefix ?? "az command failed"}: " +
                process.StandardError.ReadToEnd());
    }
}
