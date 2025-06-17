using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Topaz.CLI.Commands;
using Topaz.Service.EventHub.Commands;
using Topaz.Service.KeyVault.Commands;
using Topaz.Service.ResourceGroup.Commands;
using Topaz.Service.Storage.Commands;
using Topaz.Service.Subscription.Commands;
using Topaz.Shared;

namespace Topaz.CLI;

[UsedImplicitly]
internal class Program
{
    internal static async Task<int> Main(string[] args)
    {
        try
        {
            var registrations = new ServiceCollection();
            RegisterDependencies(registrations);

            var registrar = new TypeRegistrar(registrations);

            var result = await BootstrapCli(args, registrar);
            return result;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.Message);
            await Console.Error.WriteLineAsync(ex.StackTrace);

            return 1;
        }
    }

    private static void RegisterDependencies(ServiceCollection registrations)
    {
        registrations.AddSingleton<ITopazLogger, PrettyTopazLogger>();
    }

    private static Task<int> BootstrapCli(string[] args, TypeRegistrar registrar)
    {
        CreateLocalDirectoryForEmulator();

        var app = new CommandApp(registrar);
        
        app.Configure(config =>
        {
            config.AddCommand<StartCommand>("start");

            config.AddBranch("storage", branch =>
            {
                branch.AddBranch("account", account =>
                {
                    account.AddCommand<CreateStorageAccountCommand>("create");
                    account.AddCommand<DeleteStorageAccountCommand>("delete");
                    account.AddCommand<ShowStorageAccountConnectionStringCommand>("show-connection-string");
                    
                    account.AddBranch("keys", keys =>
                    {
                        keys.AddCommand<ListStorageAccountKeysCommand>("list");
                    });
                });

                branch.AddBranch("table", account =>
                {
                    account.AddCommand<CreateTableCommand>("create");
                    account.AddCommand<DeleteTableCommand>("delete");
                });
                
                branch.AddBranch("container", account =>
                {
                    account.AddCommand<CreateBlobContainerCommand>("create");
                });
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
            
            config.AddBranch("eventhubs", branch =>
            {
                branch.AddBranch("namespace", subbranch =>
                {
                    subbranch.AddCommand<CreateEventHubNamespaceCommand>("create");
                    subbranch.AddCommand<DeleteEventHubNamespaceCommand>("delete");
                });
                
                branch.AddBranch("eventhub", subbranch =>
                {
                    subbranch.AddCommand<CreateEventHubCommand>("create");
                    subbranch.AddCommand<DeleteEventHubCommand>("delete");
                });
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