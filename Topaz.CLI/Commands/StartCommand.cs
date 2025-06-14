using Topaz.Shared;
using Spectre.Console.Cli;

namespace Topaz.CLI.Commands;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class StartCommand(ITopazLogger logger) : Command<StartCommand.StartCommandSettings>
{
    private readonly ITopazLogger _topazLogger = logger;

    public override int Execute(CommandContext context, StartCommandSettings settings)
    {
        if (settings.LogLevel != null)
        {
            this._topazLogger.SetLoggingLevel(settings.LogLevel.Value);    
        }
        
        var host = new Topaz.Host.Host(this._topazLogger);
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
