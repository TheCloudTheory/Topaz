using Azure.Local.Shared;
using Spectre.Console.Cli;

namespace Azure.Local.CLI.Commands;

internal sealed class StartCommand(ILogger logger) : Command
{
    private readonly ILogger logger = logger;

    public override int Execute(CommandContext context)
    {
        var host = new Azure.Local.Host.Host(this.logger);
        host.Start();

        return 0;
    }
}
