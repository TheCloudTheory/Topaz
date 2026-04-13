using System.ComponentModel;
using JetBrains.Annotations;
using Spectre.Console.Cli;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Host;

[UsedImplicitly]
internal sealed class StartHostCommand : AsyncCommand<StartHostCommand.Settings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var logger = new PrettyTopazLogger();

        if (settings.LogLevel.HasValue)
        {
            logger.SetLoggingLevel(settings.LogLevel.Value);
        }

        if (settings.EnableLoggingToFile)
        {
            logger.EnableLoggingToFile(settings.RefreshLog);
            logger.LogInformation("Enabled logging to file.");
        }

        var host = new Host(new GlobalOptions
        {
            CertificateFile = settings.CertificateFile,
            CertificateKey = settings.CertificateKey,
            EnableLoggingToFile = settings.EnableLoggingToFile,
            DefaultSubscription = settings.DefaultSubscription,
            EmulatorIpAddress = settings.EmulatorIpAddress
        }, logger);

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await host.StartAsync(cts.Token);
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            return 1;
        }
    }

    internal sealed class Settings : CommandSettings
    {
        [Description("Log level. Available values: Debug, Information, Warning, Error.")]
        [CommandOption("-l|--log-level")]
        public LogLevel? LogLevel { get; set; }

        [Description("PEM-encoded certificate file for HTTPS/AMQPS (BYOC).")]
        [CommandOption("--certificate-file")]
        public string? CertificateFile { get; set; }

        [Description("PEM-encoded certificate key for HTTPS/AMQPS (BYOC).")]
        [CommandOption("--certificate-key")]
        public string? CertificateKey { get; set; }

        [Description("Save logs to a file.")]
        [CommandOption("--enable-logging-to-file")]
        public bool EnableLoggingToFile { get; set; }

        [Description("Clear the log file on startup. Enabled by default.")]
        [CommandOption("--refresh-log")]
        [DefaultValue(true)]
        public bool RefreshLog { get; set; } = true;

        [Description("Create a default subscription with the provided ID on startup.")]
        [CommandOption("--default-subscription")]
        public Guid? DefaultSubscription { get; set; }

        [Description("IP address to listen on. Defaults to 127.0.0.1.")]
        [CommandOption("--emulator-ip-address")]
        public string? EmulatorIpAddress { get; set; }
    }
}
