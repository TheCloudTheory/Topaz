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

        var host = new Topaz.Host.Host(new GlobalOptions()
        {
            TenantId = settings.TenantId
        }, logger);

        host.Start();

        return 0;
    }

    [UsedImplicitly]
    public sealed class StartCommandSettings : CommandSettings
    {
        [CommandOption("-l|--log-level")] public LogLevel? LogLevel { get; set; }

        [CommandOption("--tenant-id")] public Guid? TenantId { get; set; }
    }
}