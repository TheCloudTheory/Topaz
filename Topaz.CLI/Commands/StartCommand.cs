using Topaz.Shared;
using Spectre.Console.Cli;

namespace Topaz.CLI.Commands;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class StartCommand(ILogger logger) : Command<StartCommand.StartCommandSettings>
{
    private readonly ILogger logger = logger;

    public override int Execute(CommandContext context, StartCommandSettings settings)
    {
        if (settings.LogLevel != null)
        {
            this.logger.SetLoggingLevel(settings.LogLevel.Value);    
        }
        
        var host = new Topaz.Host.Host(this.logger);
        host.Start();

        return 0;
    }
    
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class StartCommandSettings : CommandSettings
    {
        [CommandOption("-l|--log-level")]
        public LogLevel? LogLevel { get; set; }
    }
}
