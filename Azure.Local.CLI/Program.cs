using Azure.Local.CLI;
using Azure.Local.CLI.Commands;
using Azure.Local.Service.ResourceGroup.Commands;
using Azure.Local.Service.Storage.Commands;
using Azure.Local.Shared;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

internal class Program
{
    internal static async Task<int> Main(string[] args)
    {
        try
        {
            Console.WriteLine("Azure.Local.CLI - Welcome!");

            var registrations = new ServiceCollection();
            RegisterDependencies(registrations);

            var registrar = new TypeRegistrar(registrations);

            var result = await BootstrapCli(args, registrar);
            return result;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(ex.StackTrace);

            return 1;
        }
    }

    private static void RegisterDependencies(ServiceCollection registrations)
    {
        registrations.AddSingleton<ILogger, PrettyLogger>();
    }

    private static Task<int> BootstrapCli(string[] args, TypeRegistrar registrar)
    {
        var app = new CommandApp(registrar);
        
        app.Configure(config =>
        {
            config.AddCommand<StartCommand>("start");

            config.AddBranch("storage", branch => {
                branch.AddCommand<CreateStorageAccountCommand>("create");
                branch.AddCommand<DeleteStorageAccountCommand>("delete");
            });    

            config.AddBranch("group", branch => {
                branch.AddCommand<CreateResourceGroupCommand>("create");
            });
        });

        return app.RunAsync(args);
    }
}