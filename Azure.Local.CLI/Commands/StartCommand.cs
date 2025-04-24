using Spectre.Console.Cli;

namespace Azure.Local.CLI.Commands;

internal sealed class StartCommand : Command
{
    public override int Execute(CommandContext context)
    {
        var host = new Azure.Local.Host.Host();
        host.Start();

        return 0;
    }
}
