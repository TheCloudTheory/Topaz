using Topaz.CLI;
using Topaz.CLI.Commands;
using Topaz.Service.KeyVault.Commands;
using Topaz.Service.ResourceGroup.Commands;
using Topaz.Service.Storage.Commands;
using Topaz.Service.Subscription.Commands;
using Topaz.Shared;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

internal class Program
{
    internal static async Task<int> Main(string[] args)
    {
        try
        {
            Console.WriteLine("Topaz.CLI - Welcome!");

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
        CreateLocalDirectoryForEmulator();

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
                branch.AddCommand<DeleteResourceGroupCommand>("delete");
            });

            config.AddBranch("keyvault", branch => {
                branch.AddCommand<CreateKeyVaultCommand>("create");
                branch.AddCommand<DeleteKeyVaultCommand>("delete");
            });

            config.AddBranch("subscription", branch => {
                branch.AddCommand<CreateSubscriptionCommand>("create");
                branch.AddCommand<DeleteSubscriptionCommand>("delete");
            });
        });

        return app.RunAsync(args);
    }

    private static void CreateLocalDirectoryForEmulator()
    {
        const string emulatorPath = ".topaz";
        if(Directory.Exists(emulatorPath)) return;
        
        Console.WriteLine("Creating local directory for emulator...");

        Directory.CreateDirectory(".topaz");
    }
}