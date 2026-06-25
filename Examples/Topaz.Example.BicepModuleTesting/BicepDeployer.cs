using System.Diagnostics;

namespace Topaz.Example.BicepModuleTesting;

public static class BicepDeployer
{
    private static readonly string ModulesDirectory =
        Path.Combine(AppContext.BaseDirectory, "modules");

    public static void Login()
    {
        RunAz(["login", "--username", "topazadmin@topaz.local.dev", "--password", "admin"]);
    }

    public static void Deploy(
        string resourceGroup,
        string templateFileName,
        string? parameters = null)
    {
        var templatePath = Path.Combine(ModulesDirectory, templateFileName);
        var argList = new List<string>
        {
            "deployment", "group", "create",
            "--only-show-errors",
            "--resource-group", resourceGroup,
            "--template-file", templatePath
        };

        if (parameters is not null)
        {
            argList.Add("--parameters");
            argList.AddRange(parameters.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        RunAz(argList, errorPrefix: $"Bicep deployment failed ({templateFileName})");
    }

    private static void RunAz(IEnumerable<string> argList, string? errorPrefix = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "az",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var arg in argList)
            startInfo.ArgumentList.Add(arg);

        var process = new Process { StartInfo = startInfo };

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
