using JetBrains.Annotations;
using Topaz.Shared;
using Spectre.Console.Cli;
using Topaz.Service.Shared;

namespace Topaz.CLI.Commands;

[UsedImplicitly]
internal sealed class StartCommand(ITopazLogger logger) : Command<StartCommand.StartCommandSettings>
{
    public override int Execute(CommandContext context, StartCommandSettings settings)
    {
        if (settings.LogLevel != null)
        {
            logger.SetLoggingLevel(settings.LogLevel.Value);
        }

        if (settings.TenantId == null)
        {
            logger.LogWarning(
                "No tenant specified. Using --tenant-id options required if you want to use Topaz with Azure CLI.");
        }

        if (settings.EnableLoggingToFile)
        {
            logger.EnableLoggingToFile();
            logger.LogInformation("Enabled logging to file.");
        }

        var host = new Topaz.Host.Host(new GlobalOptions
        {
            TenantId = settings.TenantId,
            CertificateFile = settings.CertificateFile,
            CertificateKey = settings.CertificateKey,
            SkipRegistrationOfDnsEntries = settings.SkipRegistrationOfDnsEntries,
            EnableLoggingToFile = settings.EnableLoggingToFile
        }, logger);

        host.Start();

        return 0;
    }

    [UsedImplicitly]
    public sealed class StartCommandSettings : CommandSettings
    {
        [CommandOption("-l|--log-level")] public LogLevel? LogLevel { get; set; }
        [CommandOption("--tenant-id")] public Guid? TenantId { get; set; }
        [CommandOption("--certificate-file")] public string? CertificateFile { get; set; }
        [CommandOption("--certificate-key")] public string? CertificateKey { get; set; }
        [CommandOption("--skip-dns-registration")] public bool SkipRegistrationOfDnsEntries { get; set; }
        [CommandOption("--enable-logging-to-file")] public bool EnableLoggingToFile { get; set; }
    }
}