using JetBrains.Annotations;
using Topaz.Shared;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;

namespace Topaz.CLI.Commands;

[UsedImplicitly]
[CommandDefinition("start", "emulator", "Starts the emulator.")]
[CommandExample("Start the emulator with default settings", "topaz start")]
[CommandExample("Start the emulator maximum verbosity", "topaz start --log-level Debug")]
[CommandExample("Start the emulator with your own certificates",
    "topaz start --certificate-file \"topaz.crt\" --certificate-key \"topaz.key\"")]
public sealed class StartCommand(ITopazLogger logger) : AsyncCommand<StartCommand.StartCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, StartCommandSettings settings)
    {
        if (settings.LogLevel != null)
        {
            logger.SetLoggingLevel(settings.LogLevel.Value);
        }

        if (settings.TenantId == null)
        {
            Console.WriteLine(
                "No tenant specified. Using --tenant-id options is required if you want to use Topaz with Azure CLI.");
        }

        if (settings.EnableLoggingToFile)
        {
            logger.EnableLoggingToFile(settings.RefreshLog);
            logger.LogInformation("Enabled logging to file.");
        }
        
        var host = new Topaz.Host.Host(new GlobalOptions
        {
            TenantId = settings.TenantId,
            CertificateFile = settings.CertificateFile,
            CertificateKey = settings.CertificateKey,
            EnableLoggingToFile = settings.EnableLoggingToFile,
            DefaultSubscription = settings.DefaultSubscription,
            EmulatorIpAddress = settings.EmulatorIpAddress
        }, logger);

        try
        {
            await host.StartAsync(Program.CancellationToken);
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            return 1;
        }
    }

    [UsedImplicitly]
    public sealed class StartCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("Sets the log level. Available values are: Debug, Information, Warning, Error")]
        [CommandOption("-l|--log-level")]
        public LogLevel? LogLevel { get; set; }

        [CommandOptionDefinition("Configures the tenant ID used when providing metadata endpoints")]
        [CommandOption("--tenant-id")]
        public Guid? TenantId { get; set; }

        [CommandOptionDefinition(
            "Allows you to bring your own certificate (BYOC). Must be an RFC 7468 PEM-encoded certificate.")]
        [CommandOption("--certificate-file")]
        public string? CertificateFile { get; set; }

        [CommandOptionDefinition("Allows you to bring your own certificate (BYOC).")]
        [CommandOption("--certificate-key")]
        public string? CertificateKey { get; set; }

        [CommandOptionDefinition("Tells the emulator to save logs to a file.")]
        [CommandOption("--enable-logging-to-file")]
        public bool EnableLoggingToFile { get; set; }

        [CommandOptionDefinition("Clears the logs file upon starting the emulator.")]
        [CommandOption("--refresh-log")]
        public bool RefreshLog { get; set; } = true;

        [CommandOptionDefinition("Creates a default subscription with the provided subscription ID")]
        [CommandOption("--default-subscription")]
        public Guid? DefaultSubscription { get; set; }

        [CommandOptionDefinition(
            "Defines the IP address used by the emulator to listen to incoming requests. Not that this address is only relevant if running the emulator directly on a host machine.")]
        [CommandOption("--emulator-ip-address")]
        public string? EmulatorIpAddress { get; set; }
    }
}