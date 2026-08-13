using System.Diagnostics;
using System.Text;
using Topaz.Shared;

namespace Topaz.CLI;

internal sealed class AzureCliRunner(ITopazLogger logger)
{
    public string? RunCommand(string command)
    {
        try
        {
            using var process = new Process();

            var output = new StringBuilder();
            string? error = null;
            
            process.StartInfo = new ProcessStartInfo("az", command)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            process.OutputDataReceived += (sender, args) => { output.Append(args.Data); };
            process.ErrorDataReceived += (sender, args) => { error = args.Data; };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error))
            {
                logger.LogError(nameof(AzureCliRunner), nameof(RunCommand), "Azure CLI command failed: {0}", error);
            }
            
            return output.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(nameof(AzureCliRunner), nameof(RunCommand), "Failed to run Azure CLI command: {0}", ex.Message);
            return null;
        }
    }
}