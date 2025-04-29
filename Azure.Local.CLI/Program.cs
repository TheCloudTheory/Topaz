using Azure.Local.CLI.Commands;
using Azure.Local.Service.Storage.Commands;
using Azure.Local.Shared;
using Spectre.Console.Cli;

internal class Program
{
    internal static async Task<int> Main(string[] args)
    {
        PrettyLogger.LogInformation("Azure.Local.CLI - Welcome!");

        var result = await BootstrapCli(args);
        return result;
    }

    private static Task<int> BootstrapCli(string[] args)
    {
        var app = new CommandApp();
        
        app.Configure(config =>
        {
            config.AddCommand<StartCommand>("start");
            config.AddBranch("storage", branch => {
                branch.AddCommand<CreateStorageAccountCommand>("create");
            });    
        });

        return app.RunAsync(args);
    }
}