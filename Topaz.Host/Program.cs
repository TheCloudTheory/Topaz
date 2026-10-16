using Spectre.Console.Cli;
using Topaz.Host.Commands;

namespace Topaz.Host;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var app = new CommandApp<StartHostCommand>();
        app.Configure(config => config.SetApplicationName("topaz-host"));
        return await app.RunAsync(args);
    }
}
