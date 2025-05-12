using Topaz.Shared;
using Spectre.Console.Cli;

namespace Topaz.CLI.Commands;

internal sealed class StartCommand(ILogger logger) : Command
{
    private readonly ILogger logger = logger;

    public override int Execute(CommandContext context)
    {
        var host = new Topaz.Host.Host(this.logger);
        host.Start();

        return 0;
    }
}
