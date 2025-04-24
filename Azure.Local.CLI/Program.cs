using Azure.Local.CLI.Commands;
using Azure.Local.Shared;
using Spectre.Console.Cli;

internal class Program
{
    private static async Task Main(string[] args)
    {
        PrettyLogger.LogInformation("Azure.Local.CLI - Welcome!");

        await BootstrapCli(args);
    }

    private static Task BootstrapCli(string[] args)
    {
        var app = new CommandApp();
        
        app.Configure(config =>
        {
            config.AddCommand<StartCommand>("start");
        });

        return app.RunAsync(args);
    }
}